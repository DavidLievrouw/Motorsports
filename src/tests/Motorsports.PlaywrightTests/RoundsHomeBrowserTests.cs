using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class RoundsHomeBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Due_round_can_be_created_and_marked_ready_to_watch(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);
        var roundName = $"PW Today {Guid.NewGuid():N}"[..16];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Round date entry and Ready to watch workflow",
            async () =>
            {
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/1/rounds/create");
                var today = await browser.Page.EvaluateAsync<string>("() => new Date().toISOString().slice(0, 10)");
                var date = browser.Page.GetByLabel("Date");
                await FillDateAsync(date, today);
                Assert.Equal(today, await date.InputValueAsync());
                var number = browser.Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Number" });
                await number.FillAsync("0");
                await number.PressAsync("Tab");
                Assert.Equal("0", await number.InputValueAsync());
                await browser.Page.GetByLabel("Name", new() { Exact = true }).FillAsync(roundName);
                await SelectAutocompleteAsync(browser.Page, "Venue", "Silverstone", "Silverstone", touchProfile);
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, "**/rounds/details/*");
                var roundId = int.Parse(
                    new Uri(browser.Page.Url).Segments[^1].TrimEnd('/'),
                    CultureInfo.InvariantCulture
                );
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync(roundName);

                await browser.NavigateInteractiveAsync(Fixture.BaseUrl);
                await browser.Page.GetByRole(AriaRole.Heading, new() { Name = "Next up", Exact = true }).WaitForAsync();
                var readyButton = browser
                    .Page.Locator(".home-round")
                    .Filter(new() { HasText = roundName })
                    .GetByRole(AriaRole.Button, new() { Name = "Ready to watch" });
                var readyResponse = browser.Page.WaitForResponseAsync(response =>
                    response.Url == $"{Fixture.BaseUrl}/rounds/{roundId}/ready-to-watch"
                    && response.Request.Method == "POST"
                );
                await ActivateAsync(readyButton, touchProfile);
                Assert.Equal(302, (await readyResponse).Status);
                await readyButton.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
