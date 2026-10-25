using System.Globalization;

namespace Motorsports.Common;

public static class RatingDisplay
{
    public static string Format(decimal? rating) =>
        rating is { } value ? $"{decimal.ToInt32(value).ToString(CultureInfo.InvariantCulture)} / 10" : "—";
}
