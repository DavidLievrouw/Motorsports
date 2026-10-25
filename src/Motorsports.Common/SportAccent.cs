namespace Motorsports.Common;

/// <summary>Subtle per-sport visual clue (a coloured left border / chip). Unknown sports get a neutral grey.</summary>
public static class SportAccent
{
    public static string Color(string? sportName) =>
        sportName?.Trim().ToLowerInvariant() switch
        {
            null or "" => "#9e9e9e",
            var n when n.Contains("formula 1", StringComparison.Ordinal) || n is "f1" => "#e10600",
            var n when n.Contains("motogp", StringComparison.Ordinal) => "#f58220",
            var n when n.Contains("indy", StringComparison.Ordinal) => "#1e88e5",
            var n
                when n.Contains("endurance", StringComparison.Ordinal) || n.Contains("wec", StringComparison.Ordinal) =>
                "#00a3a3",
            var n when n.Contains("rally", StringComparison.Ordinal) || n.Contains("wrc", StringComparison.Ordinal) =>
                "#7cb342",
            var n when n.Contains("formula e", StringComparison.Ordinal) || n is "fe" => "#d500f9",
            var n when n.Contains("nascar", StringComparison.Ordinal) => "#00c853",
            _ => "#9e9e9e",
        };

    public static string Style(string? sportName) => $"border-left: 4px solid {Color(sportName)};";

    /// <summary>Grid row style; pair with the <c>sport-accent-row</c> class to colour the row's left edge.</summary>
    public static string RowStyle(string? sportName) => $"--sport-accent: {Color(sportName)};";
}
