using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Motorsports.Common;
using Motorsports.Infrastructure.Auth;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Auth;

public static class AuthEndpoints
{
    public const string LoginPath = "/login";
    public const string LogoutPath = "/logout";
    public const string AccessDeniedPath = "/accessdenied";
    public const string ChangePasswordPath = "/account/change-password";

    // Verified for unknown users so response time does not reveal whether a user name exists.
    private static readonly User DummyUser = new();
    private static readonly string DummyPasswordHash = new PasswordHasher<User>().HashPassword(
        DummyUser,
        Guid.NewGuid().ToString()
    );

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuthServices(IConfiguration configuration)
        {
            services
                .AddOptions<AuthOptions>()
                .Bind(configuration.GetSection(AuthOptions.Section))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddValidatorsFromAssemblyContaining<LoginFormValidator>(
                ServiceLifetime.Singleton,
                includeInternalTypes: true
            );
            services
                .AddOptions<AuthProtectionOptions>()
                .Bind(configuration.GetSection(AuthProtectionOptions.Section))
                .ValidateDataAnnotations()
                .Validate(
                    limits => 3L * limits.GlobalAttempts * 60 <= 500L * limits.GlobalWindowSeconds,
                    "The global rate must allow at most 500 new reputation keys per minute so indexed cleanup can keep up."
                )
                .ValidateOnStart();
            services.AddSingleton<AuthThrottleStore>();
            services.AddSingleton<AuthProtection>();
            services.AddHostedService<AuthProtectionCleanup>();
            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseForcePasswordChange() => app.UseMiddleware<ForcePasswordChangeMiddleware>();
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapAuthEndpoints()
        {
            endpoints.MapPost(LoginPath, LoginAsync).AllowAnonymous();
            endpoints.MapPost(LogoutPath, LogoutAsync).AllowAnonymous();
            endpoints.MapPost(ChangePasswordPath, ChangePasswordAsync).RequireAuthorization();
            return endpoints;
        }
    }

    public static string LoginFailedUrl(string? returnUrl, string error = "1") =>
        LocalUrl.IsLocal(returnUrl)
            ? $"{LoginPath}?error={error}&returnUrl={Uri.EscapeDataString(returnUrl!)}"
            : $"{LoginPath}?error={error}";

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginForm form,
        HttpContext context,
        IAntiforgery antiforgery,
        AuthProtection protection,
        AuthThrottleStore throttle,
        SignInManager<User> signInManager,
        IValidator<LoginForm> validator,
        IPasswordHasher<User> passwordHasher,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken
    )
    {
        // Explicit login-CSRF defence: reject cross-site credential posts before any credential check or sign-in.
        if (!await antiforgery.IsRequestValidAsync(context))
        {
            return Results.BadRequest();
        }

        var logger = loggerFactory.CreateLogger("Motorsports.Auth.Login");
        using var lease = protection.TryEnter();
        if (!lease.IsAcquired)
        {
            return protection.Reject(context, 1);
        }
        var admission = await throttle.ReserveAsync(
            signInManager.UserManager.NormalizeName(form.Username is { Length: <= 255 } ? form.Username : ""),
            ClientAddress(context),
            protection.Device(context),
            cancellationToken
        );
        if (!admission.IsSuccess)
        {
            logger.LogWarning(
                "Authentication protection could not reserve an attempt: {Reason}",
                admission.Error.Message
            );
            return protection.Reject(context, 60, unavailable: true);
        }
        if (admission.Value.Reservation is not { } reservation)
        {
            return protection.Reject(context, admission.Value.RetryAfterSeconds);
        }
        if (!(await validator.ValidateAsync(form, cancellationToken)).IsValid)
        {
            var invalid = await throttle.CompleteAsync(
                reservation,
                AuthAttemptOutcome.InvalidInput,
                CancellationToken.None
            );
            if (!invalid.IsSuccess)
            {
                logger.LogWarning("Authentication protection completion failed: {Reason}", invalid.Error.Message);
                return protection.Reject(context, 60, unavailable: true);
            }
            return Results.Redirect(LoginFailedUrl(form.ReturnUrl));
        }

        var user = await signInManager.UserManager.FindByNameAsync(form.Username!);
        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, form.Password!);
        }
        else
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, form.Password!, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var completion = await throttle.CompleteAsync(
                    reservation,
                    AuthAttemptOutcome.Succeeded,
                    CancellationToken.None
                );
                if (!completion.IsSuccess)
                {
                    logger.LogWarning(
                        "Authentication protection completion failed: {Reason}",
                        completion.Error.Message
                    );
                    return protection.Reject(context, 60, unavailable: true);
                }
                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Redirect(
                    user.ForceChangePassword
                        ? $"{ChangePasswordPath}?returnUrl={Uri.EscapeDataString(LocalUrl.OrDefault(form.ReturnUrl))}"
                        : LocalUrl.OrDefault(form.ReturnUrl)
                );
            }
        }

        var failure = await throttle.CompleteAsync(reservation, AuthAttemptOutcome.Failed, CancellationToken.None);
        if (!failure.IsSuccess)
        {
            logger.LogWarning("Authentication protection completion failed: {Reason}", failure.Error.Message);
            return protection.Reject(context, 60, unavailable: true);
        }
        logger.LogInformation("Failed login attempt.");
        return Results.Redirect(LoginFailedUrl(form.ReturnUrl));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<User> signInManager, IFormCollection _)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect(LoginPath);
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromForm] ChangePasswordModel form,
        HttpContext context,
        AuthProtection protection,
        AuthThrottleStore throttle,
        SignInManager<User> signInManager,
        IValidator<ChangePasswordModel> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken
    )
    {
        var logger = loggerFactory.CreateLogger("Motorsports.Auth.ChangePassword");
        var redirectUrl = (string? error) =>
        {
            var url = $"{ChangePasswordPath}?returnUrl={Uri.EscapeDataString(LocalUrl.OrDefault(form.ReturnUrl))}";
            return error is null ? url : $"{url}&error={Uri.EscapeDataString(error)}";
        };

        var userManager = signInManager.UserManager;
        var user = await userManager.GetUserAsync(context.User);
        if (user is null)
        {
            return Results.Redirect(LoginPath);
        }

        using var lease = protection.TryEnter();
        if (!lease.IsAcquired)
        {
            return protection.Reject(context, 1);
        }
        var admission = await throttle.ReserveAsync(
            userManager.NormalizeName(user.Username),
            ClientAddress(context),
            protection.Device(context),
            cancellationToken
        );
        if (!admission.IsSuccess)
        {
            logger.LogWarning(
                "Authentication protection could not reserve an attempt: {Reason}",
                admission.Error.Message
            );
            return protection.Reject(context, 60, unavailable: true);
        }
        if (admission.Value.Reservation is not { } reservation)
        {
            return protection.Reject(context, admission.Value.RetryAfterSeconds);
        }
        if (!(await validator.ValidateAsync(form, cancellationToken)).IsValid)
        {
            var invalid = await throttle.CompleteAsync(
                reservation,
                AuthAttemptOutcome.InvalidInput,
                CancellationToken.None
            );
            if (!invalid.IsSuccess)
            {
                logger.LogWarning("Authentication protection completion failed: {Reason}", invalid.Error.Message);
                return protection.Reject(context, 60, unavailable: true);
            }
            return Results.Redirect(redirectUrl("invalid"));
        }

        var result = await userManager.ChangePasswordAsync(user, form.CurrentPassword!, form.NewPassword!);
        var outcome =
            result.Succeeded ? AuthAttemptOutcome.Succeeded
            : result.Errors.Any(error => error.Code == "PasswordMismatch") ? AuthAttemptOutcome.Failed
            : AuthAttemptOutcome.InvalidInput;
        var completed = await throttle.CompleteAsync(reservation, outcome, CancellationToken.None);
        if (!completed.IsSuccess)
        {
            logger.LogWarning("Authentication protection completion failed: {Reason}", completed.Error.Message);
            return protection.Reject(context, 60, unavailable: true);
        }
        if (!result.Succeeded)
        {
            var error = result.Errors.Any(identityError => identityError.Code == "PasswordMismatch")
                ? "current"
                : "failed";
            return Results.Redirect(redirectUrl(error));
        }

        if (user.ForceChangePassword)
        {
            user.ForceChangePassword = false;
            var update = await userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                logger.LogError("Password changed, but the forced password-change flag could not be cleared.");
                return Results.Problem("The password changed, but the account update failed.");
            }
        }

        await signInManager.RefreshSignInAsync(user);
        return Results.Redirect(LocalUrl.OrDefault(form.ReturnUrl));
    }

    private static string ClientAddress(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        return address is null ? "unknown" : (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }
}
