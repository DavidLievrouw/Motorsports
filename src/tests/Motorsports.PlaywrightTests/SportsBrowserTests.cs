using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class SportsBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Sport_create_validates_persists_and_removes_image(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);
        var sportName = $"PW{Guid.NewGuid():N}"[..10];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Sport validation, image, and persistence workflow",
            async () =>
            {
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");

                var createName = browser.Page.GetByLabel("Name", new() { Exact = true });
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                Assert.True(await createName.EvaluateAsync<bool>("element => element.validity.valueMissing"));
                Assert.Empty(await createName.InputValueAsync());

                await createName.FillAsync(sportName);
                await browser
                    .Page.GetByLabel("Full name", new() { Exact = true })
                    .FillAsync($" Synthetic {sportName} ");
                await browser
                    .Page.Locator("input[type=file]")
                    .SetInputFilesAsync(
                        new FilePayload
                        {
                            Name = "synthetic.png",
                            MimeType = "image/png",
                            Buffer = Convert.FromBase64String(
                                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/bXcAAAAASUVORK5CYII="
                            ),
                        }
                    );
                await browser.Page.GetByText("New image selected", new() { Exact = false }).WaitForAsync();
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await WaitForUrlAsync(browser.Page, $"**/sports/details/{sportName}");
                await browser.WaitForDomToSettleAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync($"Synthetic {sportName}");

                var image = browser.Page.GetByRole(AriaRole.Img, new() { Name = $"{sportName} logo" });
                await image.WaitForAsync();
                var imageUrl = await image.GetAttributeAsync("src");
                Assert.Equal($"/sports/image/{Uri.EscapeDataString(sportName)}", imageUrl);
                var imageResponse = await browser.Page.APIRequest.GetAsync($"{Fixture.BaseUrl}{imageUrl}");
                Assert.Equal(200, imageResponse.Status);
                Assert.StartsWith(
                    "image/png",
                    imageResponse.Headers["content-type"],
                    StringComparison.OrdinalIgnoreCase
                );

                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync($"Synthetic {sportName}");
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/edit/{sportName}");
                await browser.Page.GetByRole(AriaRole.Button, new() { Name = "Remove image" }).ClickAsync();
                await browser.Page.GetByLabel("Full name", new() { Exact = true }).FillAsync("Changed synthetic name");
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Save" }), touchProfile);
                await WaitForUrlAsync(browser.Page, $"**/sports/details/{sportName}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(browser.Page.Locator("body")).ToContainTextAsync("Changed synthetic name");
                Assert.Empty(
                    await browser.Page.GetByRole(AriaRole.Img, new() { Name = $"{sportName} logo" }).AllAsync()
                );

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");
                await browser.Page.GetByLabel("Name", new() { Exact = true }).FillAsync("F1");
                await ActivateAsync(browser.Page.GetByRole(AriaRole.Button, new() { Name = "Create" }), touchProfile);
                await browser
                    .Page.GetByText("A sport with this name already exists.", new() { Exact = true })
                    .WaitForAsync();
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
