using System.Globalization;

namespace Motorsports.Common;

public static class DateDisplay
{
    public static string Format(DateOnly date)
    {
        var weekday = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => "Su",
            DayOfWeek.Monday => "Mo",
            DayOfWeek.Tuesday => "Tu",
            DayOfWeek.Wednesday => "We",
            DayOfWeek.Thursday => "Th",
            DayOfWeek.Friday => "Fr",
            DayOfWeek.Saturday => "Sa",
            _ => date.ToString("ddd", CultureInfo.InvariantCulture)[..2],
        };
        return $"{weekday}, {date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}";
    }

    public static string Format(DateOnly? date) => date is { } value ? Format(value) : "—";

    public static bool TryParse(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateOnly.TryParseExact(value, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        var comma = value.IndexOf(',');
        return comma is 2
            && DateOnly.TryParseExact(
                value[(comma + 1)..].Trim(),
                "MMMM d, yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            );
    }
}
