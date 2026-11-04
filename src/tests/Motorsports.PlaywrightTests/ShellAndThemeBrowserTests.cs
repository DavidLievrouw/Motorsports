using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class ShellAndThemeBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Authenticated_routes_theme_and_write_flow_work(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var page = browser.Page;
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);

        try
        {
            var login = await page.GotoAsync($"{Fixture.BaseUrl}/login?returnUrl=%2Fsports%3Fsearch%3Dpw");
            Assert.Equal(200, login?.Status);
            await page.GetByLabel("Username").FillAsync(username);
            await page.GetByLabel("Password").FillAsync(PlaywrightSuiteFixture.TestPassword);
            var homeCircuit = browser.ArmCircuitWait();
            await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Log in" }), touchProfile);
            await WaitForUrlAsync(page, "**/sports?search=pw");
            await homeCircuit;
            await browser.WaitForDomToSettleAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);
            await page.GetByRole(AriaRole.Heading, new() { Name = "Sports", Exact = true }).WaitForAsync();

            var html = page.Locator("html");
            Assert.Equal("dark", await html.GetAttributeAsync("data-theme"));
            var accountButton = page.GetByRole(AriaRole.Button, new() { Name = username });
            await ActivateAsync(accountButton, touchProfile);
            await page.GetByRole(AriaRole.Menuitem, new() { Name = "Change Password" }).WaitForAsync();
            await page.Keyboard.PressAsync("Escape");
            await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Toggle theme" }), touchProfile);
            await Assertions.Expect(html).ToHaveAttributeAsync("data-theme", "light");
            Assert.Contains(
                await browser.Context.CookiesAsync(),
                cookie => cookie.Name == "theme" && cookie.Value == "light"
            );
            await browser.ReloadInteractiveAsync();
            Assert.Equal("light", await html.GetAttributeAsync("data-theme"));

            foreach (var route in BrowserTestData.AuthenticatedRoutes)
            {
                var response = await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}{route}");
                Assert.Equal(200, response?.Status);
                await page.Locator("h1").First.WaitForAsync();
                Assert.False(await page.Locator("#blazor-error-ui").IsVisibleAsync(), route);
                Assert.Equal(route, new Uri(page.Url).AbsolutePath);

                var layout = await page.EvaluateAsync<bool>(
                    "() => document.documentElement.scrollWidth > window.innerWidth + 1"
                );
                Assert.False(layout, $"{profileName} has horizontal page overflow on {route}.");
            }

            var sportName = $"PW{Guid.NewGuid():N}"[..10];
            await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");
            await page.GetByLabel("Name", new() { Exact = true }).FillAsync(sportName);
            await page.GetByLabel("Full name", new() { Exact = true }).FillAsync($"Synthetic {sportName}");
            await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
            await WaitForUrlAsync(page, $"**/sports/details/{sportName}");
            await browser.ReloadInteractiveAsync();
            await Assertions.Expect(page.Locator("body")).ToContainTextAsync(sportName);

            await page.GetByRole(AriaRole.Button, new() { Name = username }).ClickAsync();
            await ActivateAsync(page.GetByRole(AriaRole.Menuitem, new() { Name = "Log out" }), touchProfile);
            await WaitForUrlAsync(page, "**/login");
            await page.WaitForLoadStateAsync(LoadState.Load);
            await page.GotoAsync($"{Fixture.BaseUrl}/");
            await WaitForUrlAsync(page, "**/login?returnUrl=%2F");
            browser.AssertNoBrowserErrors();
        }
        catch (Exception exception)
        {
            var artifacts = await browser.RetainFailureArtifactsAsync();
            throw new InvalidOperationException(
                $"Playwright profile '{profileName}' failed. Diagnostics: {browser.DiagnosticText}. Artifacts: {artifacts}",
                exception
            );
        }
    }
}
