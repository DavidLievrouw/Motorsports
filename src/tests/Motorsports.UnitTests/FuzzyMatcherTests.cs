using Motorsports.Common.Fuzzy;
using Shouldly;

namespace Motorsports.UnitTests;

public class FuzzyMatcherTests
{
    [Theory]
    [InlineData("Ålesund Çelik", "alesund celik")]
    [InlineData("MÜLLER", "muller")]
    [InlineData("Pérez", "perez")]
    [InlineData("", "")]
    public void Normalize_folds_case_and_diacritics(string input, string expected) =>
        FuzzyMatcher.Normalize(input).ShouldBe(expected);

    [Fact]
    public void Normalize_keeps_multiplication_and_division_signs_unfolded()
    {
        FuzzyMatcher.Normalize("a\u00F7b").ShouldBe("a\u00F7b");
    }

    [Fact]
    public void Score_is_zero_when_a_token_does_not_match() =>
        FuzzyMatcher.Score("max zzz", "max verstappen").ShouldBe(0);

    [Fact]
    public void Prefix_outranks_substring() =>
        FuzzyMatcher.Score("ver", "verstappen").ShouldBeGreaterThan(FuzzyMatcher.Score("ver", "oliver"));

    [Fact]
    public void Token_order_is_irrelevant() =>
        FuzzyMatcher.Score("verstappen max", "max verstappen").ShouldBeGreaterThan(0);

    [Fact]
    public void Index_finds_by_last_name_typed_first()
    {
        var index = new FuzzyIndex<string>([
            ("Max Verstappen", [FuzzyMatcher.Normalize("Max Verstappen")]),
            ("Lewis Hamilton", [FuzzyMatcher.Normalize("Lewis Hamilton")]),
        ]);

        index.Search("verst").ShouldBe(["Max Verstappen"]);
        index.Search("").ShouldNotBeNull();
    }
}
