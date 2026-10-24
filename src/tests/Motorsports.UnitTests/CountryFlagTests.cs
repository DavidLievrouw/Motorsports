using Motorsports.Common;
using Shouldly;

namespace Motorsports.UnitTests;

public class CountryFlagTests
{
    [Theory]
    [InlineData("GB", "\U0001F1EC\U0001F1E7")]
    [InlineData("be", "\U0001F1E7\U0001F1EA")]
    [InlineData("", "")]
    [InlineData("G", "")]
    [InlineData("G1", "")]
    [InlineData(null, "")]
    public void FromIso_builds_regional_indicator_pair(string? iso, string expected) =>
        CountryFlag.FromIso(iso).ShouldBe(expected);

    [Fact]
    public void WithName_prefixes_flag_only_when_code_is_valid()
    {
        CountryFlag.WithName("BE", "Belgium").ShouldBe("\U0001F1E7\U0001F1EA Belgium");
        CountryFlag.WithName("??", "Belgium").ShouldBe("Belgium");
    }
}
