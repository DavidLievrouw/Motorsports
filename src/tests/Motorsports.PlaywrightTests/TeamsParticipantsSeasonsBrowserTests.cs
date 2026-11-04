using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class TeamsParticipantsSeasonsBrowserTests(PlaywrightSuiteFixture fixture)
    : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Team_participant_and_season_entry_workflows_persist(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);
        var teamName = $"PW Team {Guid.NewGuid():N}"[..20];
        var participantLastName = $"Montoya-{Guid.NewGuid():N}"[..16];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Team, participant, season, and entry workflows",
            async () =>
            {
                await SignInAsync(browser, username, touchProfile);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/create");
                await browser.Page.GetByLabel("Name", new() { Exact = true }).FillAsync(teamName);
                await SelectAutocompleteAsync(browser.Page, "Sport", "F1", "F1", touchProfile);
                await SelectAutocompleteAsync(browser.Page, "Country", "United", "United Kingdom", touchProfile);
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, "**/teams/details/*");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync(teamName);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/participants/create");
                await browser.Page.GetByLabel("First name", new() { Exact = true }).FillAsync("Élodie");
                await browser.Page.GetByLabel("Last name", new() { Exact = true }).FillAsync(participantLastName);
                await SelectAutocompleteAsync(browser.Page, "Country", "United", "United Kingdom", touchProfile);
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, "**/participants/details/*");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync("Élodie");
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync(participantLastName);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/create");
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await browser.Page.GetByText("Select a sport.", new() { Exact = true }).WaitForAsync();
                await SelectAutocompleteAsync(browser.Page, "Sport", "F1", "F1", touchProfile);
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, "**/seasons/details/*");
                var seasonId = int.Parse(
                    new Uri(browser.Page.Url).Segments[^1].TrimEnd('/'),
                    CultureInfo.InvariantCulture
                );
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync("F1");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/entries/create");
                await SelectAutocompleteAsync(browser.Page, "Team", teamName, teamName, touchProfile);
                var entryName = browser.Page.GetByLabel("Name during this season", new() { Exact = true });
                await Assertions.Expect(entryName).ToHaveValueAsync(teamName);
                await entryName.FillAsync("Synthetic Grand Prix Team");
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, $"**/season-entries/details/{seasonId}/*");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync("Synthetic Grand Prix Team");
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
