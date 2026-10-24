using System.ComponentModel.DataAnnotations;

namespace Motorsports.Auth;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    /// <summary>Login attempts allowed per client address and window.</summary>
    [Range(1, 1000)]
    public int LoginPermitLimit { get; set; } = 5;

    [Range(1, 3600)]
    public int LoginWindowSeconds { get; set; } = 60;

    /// <summary>Marks the auth cookie Secure. Switch off only for plain-http local development.</summary>
    public bool SecureCookies { get; set; } = true;
}
