using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class SeasonsRoundsBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    private static readonly DateOnly OldDate = new(2025, 6, 1);
    private static readonly DateOnly FutureDate = new(2027, 8, 15);

    private static ILocator Rain(IPage page) => page.GetByLabel("Rain", new() { Exact = true });

    private static async Task PickRainAsync(IPage page, string display, bool touchProfile)
    {
        await ActivateAsync(Rain(page), touchProfile);
        await ActivateAsync(page.GetByRole(AriaRole.Option, new() { Name = display, Exact = true }), touchProfile);
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_create_enforces_date_venue_and_number_rules(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.CreateSeasonAsync($"PWR-{Guid.NewGuid():N}"[..12]);
        await Fixture.CreateRoundAsync(seasonId, FutureDate, "Existing numbered");
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round create validation workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/rounds/create");

                var date = page.GetByLabel("Date");
                var number = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Number" });

                await date.FillAsync("2025-12-31");
                await number.FillAsync("0");
                await number.PressAsync("Tab");
                await SelectAutocompleteAsync(page, "Venue", "Silverstone", "Silverstone", touchProfile);
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "Date must be on or after 2026-01-01.");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/rounds/create");
                await date.FillAsync("2027-01-10");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "Select a venue.");

                await SelectAutocompleteAsync(page, "Venue", "Silverstone", "Silverstone", touchProfile);
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, "**/rounds/details/*");
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_create_rejects_duplicate_number_in_season(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round duplicate number workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/1/rounds/create");

                var number = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Number" });
                await page.GetByLabel("Date").FillAsync("2027-03-01");
                await number.FillAsync("1");
                await number.PressAsync("Tab");
                await SelectAutocompleteAsync(page, "Venue", "Silverstone", "Silverstone", touchProfile);
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "Round number must be unique in the season.");
                Assert.Contains("/rounds/create", page.Url, StringComparison.Ordinal);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_edit_only_enforces_minimum_date_when_date_changes(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.CreateSeasonAsync($"PWO-{Guid.NewGuid():N}"[..12]);
        var roundId = await Fixture.CreateRoundAsync(seasonId, OldDate, "Legacy dated round");
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round edit minimum date workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/edit/{roundId}");

                var date = page.GetByLabel("Date");
                await Assertions.Expect(date).ToHaveValueAsync("2025-06-01");
                await date.FillAsync("2025-06-02");
                await PressButtonAsync(page, "Save", touchProfile);
                await ExpectTextAsync(page, "Date must be on or after 2026-01-01.");

                await date.FillAsync("2025-06-01");
                await page.GetByLabel("Name", new() { Exact = true }).FillAsync("Legacy dated round, renamed");
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/rounds/details/{roundId}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Legacy dated round, renamed");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/edit/{roundId}");
                await page.GetByLabel("Name", new() { Exact = true }).FillAsync("Discarded");
                await ActivateAsync(
                    page.GetByRole(AriaRole.Link, new() { Name = "Cancel", Exact = true }),
                    touchProfile
                );
                await WaitForUrlAsync(page, $"**/rounds/details/{roundId}");
                await browser.ReloadInteractiveAsync();
                Assert.DoesNotContain("Discarded", await page.Locator("body").InnerTextAsync());
                browser.AssertNoBrowserErrors();
            }
        );
        await Fixture.DeleteRoundAsync(roundId);
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_finish_validates_each_outcome_then_finishes(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.FindSeasonIdAsync(PickerData.CurrentSeason);
        var roundId = await Fixture.CreateRoundAsync(seasonId, FutureDate, $"PW Finish {Guid.NewGuid():N}"[..20]);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round finish validation workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/finish/{roundId}");

                await PressButtonAsync(page, "Mark finished", touchProfile);
                await ExpectTextAsync(page, "Rating is required.");
                await ExpectTextAsync(page, "Rain is required.");
                await ExpectTextAsync(page, "Select a winning team.");
                await ExpectTextAsync(page, "Select at least one winning participant.");

                await PressButtonAsync(page, "Mark postponed", touchProfile);
                await ExpectTextAsync(page, "Note is required when postponing a round.");
                await PressButtonAsync(page, "Mark cancelled", touchProfile);
                await ExpectTextAsync(page, "Note is required when cancelling a round.");
                await PressButtonAsync(page, "Mark stopped", touchProfile);
                await ExpectTextAsync(page, "Rating is required.");
                Assert.Equal("Scheduled", (await Fixture.ReadRoundAsync(roundId)).Status);

                await page.Locator("input[type=range]").FillAsync("7");
                await Assertions.Expect(page.GetByText("Rating 7 / 10")).ToBeVisibleAsync();
                await PickRainAsync(page, "Rain without impact", touchProfile);
                await SelectAutocompleteAsync(page, "Winning team", "Modern", PickerData.ModernEntry, touchProfile);
                await SelectAutocompleteAsync(page, "Winning participants", "norris", "Lando Norris", touchProfile);
                await PressButtonAsync(page, "Mark finished", touchProfile);
                await WaitForUrlAsync(page, $"**/rounds/details/{roundId}");

                var saved = await Fixture.ReadRoundAsync(roundId);
                Assert.Equal("Finished", saved.Status);
                Assert.Equal(7, saved.Rating);
                Assert.NotNull(saved.Rain);
                Assert.Equal(1, saved.Winners);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_finish_can_postpone_with_note(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.FindSeasonIdAsync(PickerData.CurrentSeason);
        var roundId = await Fixture.CreateRoundAsync(seasonId, FutureDate, $"PW Postpone {Guid.NewGuid():N}"[..20]);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round postpone workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/finish/{roundId}");

                await page.GetByLabel("Note").FillAsync("Track flooded");
                await PressButtonAsync(page, "Mark postponed", touchProfile);
                await WaitForUrlAsync(page, $"**/rounds/details/{roundId}");

                var saved = await Fixture.ReadRoundAsync(roundId);
                Assert.Equal("Postponed", saved.Status);
                Assert.Equal("Track flooded", saved.Note);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Round_delete_is_blocked_with_winners_and_allowed_without(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.CreateSeasonAsync($"PWD-{Guid.NewGuid():N}"[..12]);
        var driver = await Fixture.FindParticipantIdAsync("Alex", "Driver");
        var protectedRound = await Fixture.CreateRoundAsync(
            seasonId,
            FutureDate,
            "Has winner",
            "Finished",
            null,
            driver
        );
        var disposableRound = await Fixture.CreateRoundAsync(seasonId, FutureDate, "Disposable");
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round delete workflow",
            async () =>
            {
                var page = browser.Page;
                var deleteButton = page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true });
                await SignInAsync(browser, username, touchProfile);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/delete/{protectedRound}");
                await ExpectTextAsync(page, "This round has winners or a winning team and cannot be deleted.");
                await Assertions.Expect(deleteButton).ToBeDisabledAsync();
                Assert.True(await Fixture.RoundExistsAsync(protectedRound));

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/rounds/delete/{disposableRound}");
                await Assertions.Expect(deleteButton).ToBeEnabledAsync();
                await ActivateAsync(deleteButton, touchProfile);
                await WaitForUrlAsync(page, $"**/seasons/{seasonId}/rounds");
                Assert.False(await Fixture.RoundExistsAsync(disposableRound));
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Season_edit_and_delete_guards(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var label = $"PWE-{Guid.NewGuid():N}"[..12];
        var emptySeason = await Fixture.CreateSeasonAsync(label);
        var seasonWithRound = await Fixture.CreateSeasonAsync($"PWF-{Guid.NewGuid():N}"[..12]);
        await Fixture.CreateRoundAsync(seasonWithRound, FutureDate, "Blocks delete");
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Season edit and delete workflow",
            async () =>
            {
                var page = browser.Page;
                var deleteButton = page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true });
                await SignInAsync(browser, username, touchProfile);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/edit/{emptySeason}");
                await Assertions.Expect(page.GetByLabel("Sport")).ToBeDisabledAsync();
                await page.GetByLabel("Label").FillAsync($"{label}-ed");
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/seasons/details/{emptySeason}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(page.Locator("body")).ToContainTextAsync($"{label}-ed");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/delete/{seasonWithRound}");
                await ExpectTextAsync(page, "A season can only be deleted when it has no rounds.");
                await Assertions.Expect(deleteButton).ToBeDisabledAsync();
                Assert.True(await Fixture.SeasonExistsAsync(seasonWithRound));

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/delete/{emptySeason}");
                await ActivateAsync(deleteButton, touchProfile);
                await WaitForUrlAsync(page, "**/seasons");
                Assert.False(await Fixture.SeasonExistsAsync(emptySeason));
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Season_entry_create_duplicate_edit_and_delete_guards(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.CreateSeasonAsync($"PWS-{Guid.NewGuid():N}"[..12]);
        var aliasTeam = await Fixture.FindTeamIdAsync(PickerData.AliasTeam);
        var otherTeam = await Fixture.FindTeamIdAsync(PickerData.OtherTeam);
        await Fixture.AddEntryAsync(seasonId, otherTeam, "Protected Racing");
        await Fixture.CreateRoundAsync(seasonId, FutureDate, "Won by entry", "Finished", otherTeam);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);
        var entryName = $"PW Entry {Guid.NewGuid():N}"[..20];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Season entry workflow",
            async () =>
            {
                var page = browser.Page;
                var deleteButton = page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true });
                await SignInAsync(browser, username, touchProfile);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/entries/create");
                await SelectAutocompleteAsync(page, "Team", PickerData.AliasTeam, PickerData.AliasTeam, touchProfile);
                await page.GetByLabel("Name during this season").FillAsync(entryName);
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, $"**/season-entries/details/{seasonId}/{aliasTeam}");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/entries/create");
                await SelectAutocompleteAsync(page, "Team", PickerData.AliasTeam, PickerData.AliasTeam, touchProfile);
                await page.GetByLabel("Name during this season").FillAsync("Second attempt");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "This team already has an entry in the season.");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/season-entries/edit/{seasonId}/{aliasTeam}");
                await page.GetByLabel("Name during this season").FillAsync($"{entryName}-ed");
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/season-entries/details/{seasonId}/{aliasTeam}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(page.Locator("body")).ToContainTextAsync($"{entryName}-ed");

                await browser.NavigateInteractiveAsync(
                    $"{Fixture.BaseUrl}/season-entries/delete/{seasonId}/{otherTeam}"
                );
                await ExpectTextAsync(page, "A season entry that has won a season or round cannot be deleted.");
                await Assertions.Expect(deleteButton).ToBeDisabledAsync();

                await browser.NavigateInteractiveAsync(
                    $"{Fixture.BaseUrl}/season-entries/delete/{seasonId}/{aliasTeam}"
                );
                await ActivateAsync(deleteButton, touchProfile);
                await WaitForUrlAsync(page, $"**/seasons/{seasonId}/entries");
                await browser.ReloadInteractiveAsync();
                Assert.DoesNotContain($"{entryName}-ed", await page.Locator("body").InnerTextAsync());
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
