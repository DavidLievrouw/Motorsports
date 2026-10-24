using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure.Auth;

public static class AuthExtensions
{
    public const int MinimumPasswordLength = 8;

    /// <summary>Identity core over the legacy User table. The host adds cookies and the sign-in manager.</summary>
    public static IdentityBuilder AddMotorsportsIdentity(this IServiceCollection services)
    {
        // Registered first so Identity's default hasher does not win.
        services.TryAddSingleton<IPasswordHasher<User>, LegacyPasswordHasher>();
        return services
            .AddIdentityCore<User>(o =>
            {
                o.Password.RequiredLength = MinimumPasswordLength;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                // Legacy user names are free text; do not reject them when a rehash updates the row.
                o.User.AllowedUserNameCharacters = "";
            })
            .AddUserStore<UserStore>()
            .AddClaimsPrincipalFactory<UserClaimsFactory>();
    }
}
