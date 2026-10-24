namespace Motorsports.Common;

public static class CountryFlag
{
    /// <summary>Flag emoji (regional indicator pair) for an ISO 3166-1 alpha-2 code; empty when the code is not two ASCII letters.</summary>
    public static string FromIso(string? iso)
    {
        if (iso is not { Length: 2 } || !char.IsAsciiLetter(iso[0]) || !char.IsAsciiLetter(iso[1]))
        {
            return string.Empty;
        }

        return string.Create(
            4,
            iso,
            static (span, code) =>
            {
                char.ConvertFromUtf32(0x1F1E6 + (char.ToUpperInvariant(code[0]) - 'A')).CopyTo(span);
                char.ConvertFromUtf32(0x1F1E6 + (char.ToUpperInvariant(code[1]) - 'A')).CopyTo(span[2..]);
            }
        );
    }

    public static string WithName(string? iso, string name) =>
        FromIso(iso) is { Length: > 0 } flag ? $"{flag} {name}" : name;
}
