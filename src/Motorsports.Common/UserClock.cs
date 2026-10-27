namespace Motorsports.Common;

/// <summary>
/// The user's "today". Scoped per circuit; the browser time zone is applied after first render,
/// until then (and when unavailable) the configured fallback zone is used. Never use for validation or writes.
/// </summary>
public sealed class UserClock(TimeProvider timeProvider, TimeZoneInfo fallbackZone)
{
    private TimeZoneInfo _zone = fallbackZone;

    public TimeZoneInfo Zone => _zone;

    /// <summary>
    /// Resolves an IANA id. With InvariantGlobalization on Windows (dev only) IANA ids are unknown, so fall back
    /// to the machine zone; on Linux (tzdata installed) ids always resolve.
    /// </summary>
    public static TimeZoneInfo ResolveZone(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Noop
            }
            catch (InvalidTimeZoneException)
            {
                // Noop
            }
        }

        return TimeZoneInfo.Local;
    }

    public DateOnly Today => TodayAt(timeProvider.GetUtcNow());

    public DateOnly TodayAt(DateTimeOffset utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, _zone).DateTime);

    /// <summary>Applies an IANA/Windows zone id from the browser; ignores unknown ids.</summary>
    public bool TrySetZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        try
        {
            _zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
