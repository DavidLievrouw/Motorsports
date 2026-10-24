using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public class DateDisplayTests
{
    [Theory]
    [InlineData(2024, 9, 1, "Su, September 1, 2024")]
    [InlineData(2024, 9, 2, "Mo, September 2, 2024")]
    [InlineData(2024, 9, 3, "Tu, September 3, 2024")]
    [InlineData(2024, 9, 4, "We, September 4, 2024")]
    [InlineData(2024, 9, 5, "Th, September 5, 2024")]
    [InlineData(2024, 9, 6, "Fr, September 6, 2024")]
    [InlineData(2024, 9, 7, "Sa, September 7, 2024")]
    public void Format_prefixes_two_letter_weekday(int year, int month, int day, string expected) =>
        DateDisplay.Format(new DateOnly(year, month, day)).ShouldBe(expected);

    [Fact]
    public void Format_returns_dash_when_missing() => DateDisplay.Format(null).ShouldBe("—");
}
