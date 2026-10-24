using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public class RainLevelDisplayTests
{
    [Theory]
    [InlineData(0, "No rain")]
    [InlineData(1, "No rain, but damp start")]
    [InlineData(2, "Rain without impact")]
    [InlineData(3, "Rain with minor impact")]
    [InlineData(4, "Rain with considerable impact")]
    [InlineData(5, "Full wet event")]
    [InlineData(6, "Stopped due to heavy rain")]
    [InlineData(7, "Cancelled due to heavy rain")]
    public void Format_maps_stored_values_to_legacy_display_names(int stored, string expected) =>
        RainLevelDisplay.Format(stored).ShouldBe(expected);

    [Fact]
    public void Format_returns_null_when_missing_or_unknown()
    {
        RainLevelDisplay.Format(null).ShouldBeNull();
        RainLevelDisplay.Format(9).ShouldBeNull();
    }
}
