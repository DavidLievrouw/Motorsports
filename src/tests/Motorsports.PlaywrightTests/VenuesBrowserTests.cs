using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class VenuesBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Venue_country_picker_and_unicode_key_survive_create_edit(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);
        var venueName = $"PW Montréal {Guid.NewGuid():N}"[..17];
        var editedName = $"{venueName} Circuit";

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Venue picker, Unicode key, and persistence workflow",
            async () =>
            {
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/venues/create");
                await browser.Page.GetByLabel("Name", new() { Exact = true }).FillAsync(venueName);
                await SelectAutocompleteAsync(browser.Page, "Country", "United", "United Kingdom", touchProfile);
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, $"**/venues/details/{Uri.EscapeDataString(venueName)}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync(venueName);

                await browser.NavigateInteractiveAsync(
                    $"{Fixture.BaseUrl}/venues/edit/{Uri.EscapeDataString(venueName)}"
                );
                await browser.Page.GetByLabel("Name", new() { Exact = true }).FillAsync(editedName);
                await browser.Page.GetByLabel("Name", new() { Exact = true }).PressAsync("Tab");
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Save" }), touchProfile);
                await WaitForUrlAsync(browser.Page, $"**/venues/details/{Uri.EscapeDataString(editedName)}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync(editedName);
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
