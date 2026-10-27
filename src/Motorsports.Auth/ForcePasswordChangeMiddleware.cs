using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Motorsports.Common;
using Motorsports.Infrastructure.Auth;

namespace Motorsports.Auth;

/// <summary>Keeps users flagged with ForceChangePassword on the change-password page until they changed it.</summary>
public sealed class ForcePasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPrefixes =
    [
        AuthEndpoints.ChangePasswordPath,
        AuthEndpoints.LogoutPath,
        AuthEndpoints.LoginPath,
        AuthEndpoints.AccessDeniedPath,
        "/health",
        "/_framework",
        "/_content",
        "/_blazor",
    ];

    public Task InvokeAsync(HttpContext context)
    {
        if (
            context.User.Identity?.IsAuthenticated != true
            || !context.User.HasClaim(AppClaimTypes.ForceChangePassword, "true")
            || IsAllowed(context.Request.Path)
        )
        {
            return next(context);
        }

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var target = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect(
                $"{AuthEndpoints.ChangePasswordPath}?returnUrl={Uri.EscapeDataString(LocalUrl.OrDefault(target))}"
            );
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }

        return Task.CompletedTask;
    }

    private static bool IsAllowed(PathString path)
    {
        // Static files (css, js, images, favicon) are harmless and needed to render the page.
        if (Path.HasExtension(path.Value))
        {
            return true;
        }

        return AllowedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
