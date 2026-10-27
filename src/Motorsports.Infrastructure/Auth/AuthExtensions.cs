using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure.Auth;

public static class AuthExtensions
{
    public const int MinimumPasswordLength = 8;

    /// <summary>Identity core over the User table. The host adds cookies and the sign-in manager.</summary>
    public static IdentityBuilder AddMotorsportsIdentity(this IServiceCollection services)
    {
        return services
            .AddIdentityCore<User>(o =>
            {
                o.Password.RequiredLength = MinimumPasswordLength;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                // User names are free text; password updates must not reject existing names.
                o.User.AllowedUserNameCharacters = "";
            })
            .AddUserStore<UserStore>()
            .AddClaimsPrincipalFactory<UserClaimsFactory>();
    }
}
