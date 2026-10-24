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
}
