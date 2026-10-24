using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure.Auth;

public static class AppClaimTypes
{
    public const string ForceChangePassword = "ms:force-change-password";
    public const string DisplayName = "ms:display-name";
}

/// <summary>Adds the force-change-password flag and a display name to the cookie principal.</summary>
public sealed class UserClaimsFactory(UserManager<User> userManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<User>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.ForceChangePassword)
        {
            identity.AddClaim(new Claim(AppClaimTypes.ForceChangePassword, "true"));
        }

        var display = string.Join(
            ' ',
            new[] { user.GivenName, user.FamilyName }.Where(x => !string.IsNullOrWhiteSpace(x))
        );
        identity.AddClaim(new Claim(AppClaimTypes.DisplayName, display.Length > 0 ? display : user.Username));
        return identity;
    }
}
