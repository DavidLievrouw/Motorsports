using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public class UserClockTests
{
    private static readonly TimeZoneInfo Plus10 = TimeZoneInfo.CreateCustomTimeZone(
        "p10",
        TimeSpan.FromHours(10),
        "p10",
        "p10"
    );

    [Fact]
    public void Today_follows_the_zone_not_utc()
    {
        var clock = new UserClock(TimeProvider.System, Plus10);
        clock.TodayAt(new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero)).ShouldBe(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public void Unknown_zone_is_ignored()
    {
        var clock = new UserClock(TimeProvider.System, Plus10);
        clock.TrySetZone("Not/AZone").ShouldBeFalse();
        clock.Zone.ShouldBe(Plus10);
    }

    [Fact]
    public void ResolveZone_never_throws() => UserClock.ResolveZone("Not/AZone").ShouldNotBeNull();
}
