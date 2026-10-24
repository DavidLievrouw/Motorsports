using System.Globalization;

namespace Motorsports.Common;

public static class RoundLabel
{
    public static string Format(short number, string? name)
    {
        var numberText = number.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(name) ? numberText : $"{numberText} - {name.Trim()}";
    }
}
