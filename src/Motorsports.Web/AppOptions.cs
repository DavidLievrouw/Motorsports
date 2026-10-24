using System.ComponentModel.DataAnnotations;

namespace Motorsports.Web;

public sealed class AppOptions
{
    public const string Section = "App";

    /// <summary>Fallback IANA time zone for "today" when the browser does not report one.</summary>
    [Required]
    public string TimeZone { get; set; } = "Europe/Brussels";
}

public sealed record BuildInfo(string Version)
{
    public string Display => Version.Length > 0 ? Version : "dev";
}
