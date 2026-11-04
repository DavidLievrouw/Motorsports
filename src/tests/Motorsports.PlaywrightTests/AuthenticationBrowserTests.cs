using System.Globalization;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class AuthenticationBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Forced_password_change_completes_in_each_browser_profile(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync(forcePasswordChange: true);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var page = browser.Page;
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);

        try
        {
            await page.GotoAsync($"{Fixture.BaseUrl}/login");
            await page.GetByLabel("Username").FillAsync(username);
            await page.GetByLabel("Password").FillAsync(PlaywrightSuiteFixture.TestPassword);
            var changePasswordCircuit = browser.ArmCircuitWait();
            await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Log in" }), touchProfile);
            await WaitForUrlAsync(page, "**/account/change-password?returnUrl=%2F");
            await changePasswordCircuit;
            await browser.WaitForDomToSettleAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);
            await Assertions.Expect(page.Locator("body")).ToContainTextAsync("You must choose a new password");

            const string newPassword = "New synthetic browser password!";
            var currentPassword = page.GetByLabel("Current password");
            var newPasswordInput = page.GetByLabel("New password", new() { Exact = true });
            var confirmPassword = page.GetByLabel("Confirm new password");
            await currentPassword.FillAsync(PlaywrightSuiteFixture.TestPassword);
            await newPasswordInput.FillAsync(newPassword);
            await confirmPassword.FillAsync(newPassword);
            Assert.Equal(PlaywrightSuiteFixture.TestPassword, await currentPassword.InputValueAsync());
            Assert.Equal(newPassword, await newPasswordInput.InputValueAsync());
            Assert.Equal(newPassword, await confirmPassword.InputValueAsync());
            var form = page.Locator("form").Last;
            var invalidFields = await form.EvaluateAsync<string>(
                "form => Array.from(form.elements).filter(element => element.willValidate && !element.validity.valid).map(element => `${element.name}: ${element.validationMessage}`).join('; ')"
            );
            Assert.True(string.IsNullOrEmpty(invalidFields), invalidFields);
            var homeCircuit = browser.ArmCircuitWait();
            var passwordChangeResponse = page.WaitForResponseAsync(response =>
                response.Url == $"{Fixture.BaseUrl}/account/change-password" && response.Request.Method == "POST"
            );
            await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Change password" }), touchProfile);
            Assert.Equal(302, (await passwordChangeResponse).Status);
            await WaitForUrlAsync(page, "**/");
            await homeCircuit;
            await browser.WaitForDomToSettleAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);
            await page.GetByRole(AriaRole.Heading, new() { Name = "Next up", Exact = true }).WaitForAsync();

            Assert.Equal(404, (await page.APIRequest.GetAsync($"{Fixture.BaseUrl}/logout")).Status);
            await page.GotoAsync($"{Fixture.BaseUrl}/");
            await page.GetByRole(AriaRole.Heading, new() { Name = "Next up", Exact = true }).WaitForAsync();
            browser.AssertNoBrowserErrors();
        }
        catch (Exception exception)
        {
            var artifacts = await browser.RetainFailureArtifactsAsync();
            throw new InvalidOperationException(
                $"Forced password change failed in '{profileName}'. Diagnostics: {browser.DiagnosticText}. Artifacts: {artifacts}",
                exception
            );
        }
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Login_rejects_untrusted_return_urls_and_shows_generic_failures(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);
        var invalidReturnUrls = new[]
        {
            "https://example.invalid/",
            "//example.invalid/",
            "/\\example.invalid/",
            "javascript:alert(1)",
        };

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Login return-URL and generic-error cases",
            async () =>
            {
                var page = browser.Page;
                foreach (var returnUrl in invalidReturnUrls)
                {
                    if (page.Url != "about:blank")
                    {
                        await browser.ResetPageAsync();
                        page = browser.Page;
                    }

                    var username = await Fixture.CreateUserAsync();
                    await page.GotoAsync($"{Fixture.BaseUrl}/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    await page.GetByLabel("Username").FillAsync(username);
                    await page.GetByLabel("Password").FillAsync(PlaywrightSuiteFixture.TestPassword);
                    var homeCircuit = browser.ArmCircuitWait();
                    await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Log in" }), touchProfile);
                    await WaitForUrlAsync(page, "**/");
                    await homeCircuit;
                    await browser.WaitForDomToSettleAsync();
                    await page.WaitForLoadStateAsync(LoadState.Load);
                    Assert.Equal("/", new Uri(page.Url).AbsolutePath);
                }

                await browser.ResetPageAsync();
                var invalidPage = browser.Page;
                await invalidPage.GotoAsync($"{Fixture.BaseUrl}/login");
                await invalidPage.GetByLabel("Username").FillAsync($"unknown-{Guid.NewGuid():N}");
                await invalidPage.GetByLabel("Password").FillAsync("Incorrect synthetic password!");
                await ActivateAsync(invalidPage.GetByRole(AriaRole.Button, new() { Name = "Log in" }), touchProfile);
                await WaitForUrlAsync(invalidPage, "**/login?error=*");
                Assert.Contains("Invalid username or password.", await invalidPage.Locator("body").InnerTextAsync());
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Invalid_logout_posts_preserve_the_authenticated_session(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile =
            profileName.StartsWith("Pixel", StringComparison.Ordinal)
            || profileName.StartsWith("iPhone", StringComparison.Ordinal);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Logout antiforgery workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);

                var missingToken = await page.APIRequest.PostAsync($"{Fixture.BaseUrl}/logout");
                Assert.Equal(400, missingToken.Status);
                foreach (var token in new[] { "", "invalid-token" })
                {
                    var invalidToken = await page.APIRequest.PostAsync(
                        $"{Fixture.BaseUrl}/logout",
                        new APIRequestContextOptions
                        {
                            Data = $"__RequestVerificationToken={Uri.EscapeDataString(token)}",
                            Headers = new Dictionary<string, string>
                            {
                                ["Content-Type"] = "application/x-www-form-urlencoded",
                            },
                        }
                    );
                    Assert.Equal(400, invalidToken.Status);
                }

                var homeResponse = await page.APIRequest.GetAsync(Fixture.BaseUrl);
                Assert.Equal(200, homeResponse.Status);
                Assert.Equal("/", new Uri(homeResponse.Url).AbsolutePath);

                Assert.Equal(404, (await page.APIRequest.GetAsync($"{Fixture.BaseUrl}/logout")).Status);
                await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = username }), touchProfile);
                await ActivateAsync(page.GetByRole(AriaRole.Menuitem, new() { Name = "Log out" }), touchProfile);
                await WaitForUrlAsync(page, "**/login");
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
