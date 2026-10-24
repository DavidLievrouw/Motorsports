using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public const string LoginRateLimitPolicy = "login";

    // Verified for unknown users so response time does not reveal whether a user name exists.
    private static readonly User DummyUser = new()
    {
        Prf = "HMACSHA512",
        Iterations = 10_000,
        Salt = new byte[16],
        PasswordHash = new byte[32],
    };

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuthServices(IConfiguration configuration)
        {
            services
                .AddOptions<AuthOptions>()
                .Bind(configuration.GetSection(AuthOptions.Section))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddValidatorsFromAssemblyContaining<LoginFormValidator>(ServiceLifetime.Singleton);
            services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy(
                    LoginRateLimitPolicy,
                    context =>
                    {
                        var limits = context.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value;
                        return RateLimitPartition.GetFixedWindowLimiter(
                            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = limits.LoginPermitLimit,
                                Window = TimeSpan.FromSeconds(limits.LoginWindowSeconds),
                                QueueLimit = 0,
                            }
                        );
                    }
                );
            });
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
            endpoints.MapPost(LoginPath, LoginAsync).AllowAnonymous().RequireRateLimiting(LoginRateLimitPolicy);
            endpoints.MapPost(LogoutPath, LogoutAsync).AllowAnonymous();
            return endpoints;
        }
    }

    public static string LoginFailedUrl(string? returnUrl, string error = "1") =>
        LocalUrl.IsLocal(returnUrl)
            ? $"{LoginPath}?error={error}&returnUrl={Uri.EscapeDataString(returnUrl!)}"
            : $"{LoginPath}?error={error}";

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginForm form,
        SignInManager<User> signInManager,
        IValidator<LoginForm> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken
    )
    {
        var logger = loggerFactory.CreateLogger("Motorsports.Auth.Login");
        if (!(await validator.ValidateAsync(form, cancellationToken)).IsValid)
        {
            return Results.Redirect(LoginFailedUrl(form.ReturnUrl));
        }

        var user = await signInManager.UserManager.FindByNameAsync(form.Username!);
        if (user is null)
        {
            LegacyPasswordHasher.VerifyLegacy(DummyUser, form.Password!);
        }
        else
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, form.Password!, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Redirect(
                    user.ForceChangePassword
                        ? $"{ChangePasswordPath}?returnUrl={Uri.EscapeDataString(LocalUrl.OrDefault(form.ReturnUrl))}"
                        : LocalUrl.OrDefault(form.ReturnUrl)
                );
            }
        }

        logger.LogInformation("Failed login attempt.");
        return Results.Redirect(LoginFailedUrl(form.ReturnUrl));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<User> signInManager, IFormCollection _)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect(LoginPath);
    }
}
