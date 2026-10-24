namespace Motorsports.Web.Components.Shared;

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
            var n when n.Contains("formula e", StringComparison.Ordinal) => "#3d5afe",
            _ => "#9e9e9e",
        };

    public static string Style(string? sportName) => $"border-left: 4px solid {Color(sportName)};";
}
