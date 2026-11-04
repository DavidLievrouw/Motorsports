using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class PickersBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    private static async Task<ILocator> SearchAsync(IPage page, string label, string query)
    {
        await page.GetByLabel(label, new() { Exact = true }).FillAsync(query);
        return page.GetByRole(AriaRole.Option);
    }

    private static ILocator OptionWithText(ILocator options, string text) =>
        options.Filter(new() { HasTextRegex = new Regex(Regex.Escape(text)) });

    private async Task<int> NewFinishRoundAsync(string seasonLabel)
    {
        var seasonId = await Fixture.FindSeasonIdAsync(seasonLabel);
        return await Fixture.CreateRoundAsync(
            seasonId,
            new DateOnly(2027, 9, 1),
            $"PW Picker {Guid.NewGuid():N}"[..20]
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Participant_picker_matches_fuzzily_and_disambiguates(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var roundId = await NewFinishRoundAsync(PickerData.CurrentSeason);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Participant picker fuzzy search workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/finish/{roundId}");
                const string label = "Winning participants";

                foreach (var query in new[] { "verst", "ver max", "VERSTAPPEN", "verstapen", "Max V" })
                {
                    var options = await SearchAsync(page, label, query);
                    await Assertions.Expect(OptionWithText(options, "Max Verstappen")).ToHaveCountAsync(1);
                }

                foreach (var query in new[] { "zoe muller", "Müller Zoë", "ZOË" })
                {
                    var options = await SearchAsync(page, label, query);
                    await Assertions.Expect(OptionWithText(options, "Zoë Müller")).ToHaveCountAsync(1);
                }

                var lastNameFirst = await SearchAsync(page, label, "norris lando");
                await Assertions.Expect(OptionWithText(lastNameFirst, "Lando Norris")).ToHaveCountAsync(1);

                var sam = await SearchAsync(page, label, "sam smith");
                await Assertions.Expect(OptionWithText(sam, "Sam Smith")).ToHaveCountAsync(2);
                await Assertions.Expect(OptionWithText(sam, "United Kingdom")).ToHaveCountAsync(1);
                await Assertions.Expect(OptionWithText(sam, "Netherlands")).ToHaveCountAsync(1);

                var unique = await SearchAsync(page, label, "norris");
                await Assertions.Expect(OptionWithText(unique, "Lando Norris")).ToHaveCountAsync(1);
                Assert.DoesNotContain("United Kingdom", await OptionWithText(unique, "Lando Norris").InnerTextAsync());

                var none = await SearchAsync(page, label, "qqqqzzzz");
                await Assertions.Expect(none).ToHaveCountAsync(0);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Participant_multi_picker_caps_at_four_and_recovers_after_removal(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var roundId = await NewFinishRoundAsync(PickerData.CurrentSeason);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Participant multi-select cap workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/finish/{roundId}");
                const string label = "Winning participants";
                var picker = page.GetByLabel(label, new() { Exact = true });
                var chips = page.Locator(".mud-chip");

                var picked = 0;
                foreach (var name in new[] { "alpha", "beta", "gamma", "delta" })
                {
                    await SelectAutocompleteAsync(page, label, $"Zed Picker{name}", $"Zed Picker{name}", touchProfile);
                    await Assertions.Expect(chips).ToHaveCountAsync(++picked);
                    if (picked < 4)
                    {
                        await picker.ClearAsync();
                    }
                }

                await Assertions.Expect(chips).ToHaveCountAsync(4);
                await Assertions.Expect(picker).ToBeDisabledAsync();

                var alpha = chips.Filter(new() { HasText = "Zed Pickeralpha" });
                await ActivateAsync(alpha.Locator(".mud-chip-close-button"), touchProfile);
                await Assertions.Expect(chips).ToHaveCountAsync(3);
                await Assertions.Expect(picker).ToBeEnabledAsync();

                var again = await SearchAsync(page, label, "Pickeralpha");
                await Assertions.Expect(OptionWithText(again, "Zed Pickeralpha")).ToHaveCountAsync(1);
                var taken = await SearchAsync(page, label, "Pickerbeta");
                await Assertions.Expect(OptionWithText(taken, "Zed Pickerbeta")).ToHaveCountAsync(0);

                await SelectAutocompleteAsync(page, label, "Zed Pickerepsilon", "Zed Pickerepsilon", touchProfile);
                await Assertions.Expect(chips).ToHaveCountAsync(4);
                await Assertions.Expect(picker).ToBeDisabledAsync();
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Winning_team_picker_only_matches_entry_names_of_that_season(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var roundId = await NewFinishRoundAsync(PickerData.CurrentSeason);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Winning team picker scoping workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/finish/{roundId}");
                const string label = "Winning team";

                var modern = await SearchAsync(page, label, "modern");
                await Assertions.Expect(OptionWithText(modern, PickerData.ModernEntry)).ToHaveCountAsync(1);
                await Assertions.Expect(modern).ToHaveCountAsync(1);

                var other = await SearchAsync(page, label, "other rac");
                await Assertions.Expect(OptionWithText(other, PickerData.OtherEntry)).ToHaveCountAsync(1);

                var oldName = await SearchAsync(page, label, "legacy");
                await Assertions.Expect(oldName).ToHaveCountAsync(0);

                var originalName = await SearchAsync(page, label, PickerData.AliasTeam);
                await Assertions.Expect(originalName).ToHaveCountAsync(0);

                await SelectAutocompleteAsync(page, label, "modern", PickerData.ModernEntry, touchProfile);
                await Assertions
                    .Expect(page.GetByLabel(label, new() { Exact = true }))
                    .ToHaveValueAsync(new Regex(Regex.Escape(PickerData.ModernEntry)));
                await ActivateAsync(page.Locator(".mud-input-clear-button").First, touchProfile);
                await Assertions.Expect(page.GetByLabel(label, new() { Exact = true })).ToHaveValueAsync("");
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Season_entry_team_picker_matches_history_names_and_filters_by_sport(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.FindSeasonIdAsync(PickerData.EmptySeason);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Season entry team picker workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/entries/create");
                const string label = "Team";

                foreach (var query in new[] { "legacy", "modern", PickerData.AliasTeam })
                {
                    var options = await SearchAsync(page, label, query);
                    await Assertions.Expect(OptionWithText(options, PickerData.AliasTeam)).ToHaveCountAsync(1);
                }

                var moto = await SearchAsync(page, label, "Picker Moto");
                await Assertions.Expect(OptionWithText(moto, PickerData.MotoTeam)).ToHaveCountAsync(0);

                await SelectAutocompleteAsync(page, label, "legacy", PickerData.AliasTeam, touchProfile);
                await Assertions.Expect(page.GetByText(PickerData.LegacyEntry).First).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText(PickerData.ModernEntry).First).ToBeVisibleAsync();
                await Assertions
                    .Expect(page.GetByLabel("Name during this season"))
                    .ToHaveValueAsync(new Regex("^(Legacy|Modern) Racing$"));
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
