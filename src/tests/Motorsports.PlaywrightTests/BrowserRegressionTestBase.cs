using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

public static class BrowserTestData
{
    public static TheoryData<string> Profiles =>
        [
            "Chromium desktop",
            "Firefox desktop",
            "WebKit desktop",
            "Pixel 10 Pro Chromium",
            "iPhone 16 Pro WebKit",
            "360x640 guard",
        ];

    public static readonly string[] AuthenticatedRoutes =
    [
        "/",
        "/sports",
        "/sports/create",
        "/sports/details/F1",
        "/sports/edit/F1",
        "/venues",
        "/venues/create",
        "/venues/details/Silverstone",
        "/venues/edit/Silverstone",
        "/teams",
        "/teams/create",
        "/teams/details/1",
        "/teams/edit/1",
        "/teams/delete/1",
        "/participants",
        "/participants/create",
        "/participants/details/1",
        "/participants/edit/1",
        "/seasons",
        "/seasons/create",
        "/seasons/details/1",
        "/seasons/edit/1",
        "/seasons/delete/1",
        "/seasons/1/entries",
        "/seasons/1/entries/create",
        "/season-entries/details/1/1",
        "/season-entries/edit/1/1",
        "/season-entries/delete/1/1",
        "/seasons/1/rounds",
        "/seasons/1/rounds/create",
        "/rounds/details/1",
        "/rounds/edit/1",
        "/rounds/finish/1",
        "/rounds/delete/1",
        "/account/change-password",
    ];
}

public abstract class BrowserRegressionTestBase(PlaywrightSuiteFixture fixture)
{
    protected PlaywrightSuiteFixture Fixture { get; } = fixture;

    protected async Task SignInAsync(BrowserSession browser, string username, bool touchProfile)
    {
        var page = browser.Page;
        await page.GotoAsync($"{Fixture.BaseUrl}/login");
        await page.GetByLabel("Username").FillAsync(username);
        await page.GetByLabel("Password").FillAsync(PlaywrightSuiteFixture.TestPassword);
        var circuit = browser.ArmCircuitWait();
        await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Log in" }), touchProfile);
        await WaitForUrlAsync(page, "**/");
        await circuit;
        await browser.WaitForDomToSettleAsync();
        await page.WaitForLoadStateAsync(LoadState.Load);
    }

    protected static async Task SelectAutocompleteAsync(
        IPage page,
        string label,
        string query,
        string expectedText,
        bool touchProfile
    )
    {
        var picker = page.GetByLabel(label);
        await picker.FillAsync(query);
        var option = page.GetByRole(AriaRole.Option)
            .Filter(new() { HasTextRegex = new Regex($@"\b{Regex.Escape(expectedText)}\b") });
        try
        {
            await option.WaitForAsync(new() { Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            await picker.FillAsync("");
            await picker.FillAsync(query);
            await option.WaitForAsync();
        }
        await ActivateAsync(option, touchProfile);
    }

    protected static async Task ExecuteWithDiagnosticsAsync(BrowserSession browser, string caseName, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            var artifacts = await browser.RetainFailureArtifactsAsync();
            throw new InvalidOperationException(
                $"{caseName} failed in '{browser.ProfileName}'. Diagnostics: {browser.DiagnosticText}. Artifacts: {artifacts}",
                exception
            );
        }
    }

    // Committing the picker text makes the server move focus back to the date input; wait for that
    // so it cannot steal focus from (and redirect typing away from) the next field.
    protected static async Task FillDateAsync(ILocator date, string value)
    {
        await date.FillAsync(value);
        await date.BlurAsync();
        await Assertions.Expect(date).ToBeFocusedAsync();
    }

    protected static Task ActivateAsync(ILocator locator, bool touchProfile) =>
        touchProfile ? locator.TapAsync() : locator.ClickAsync();

    protected static bool IsTouchProfile(string profileName) =>
        profileName.StartsWith("Pixel", StringComparison.Ordinal)
        || profileName.StartsWith("iPhone", StringComparison.Ordinal);

    protected static Task PressButtonAsync(IPage page, string name, bool touchProfile) =>
        ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }), touchProfile);

    protected static Task ExpectTextAsync(IPage page, string text) =>
        Assertions.Expect(page.GetByText(text, new() { Exact = true })).ToBeVisibleAsync();

    protected static async Task<int> LastUrlSegmentAsync(IPage page)
    {
        await page.WaitForURLAsync(
            new Regex(@"/\d+/?$"),
            new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit }
        );
        return int.Parse(new Uri(page.Url).Segments[^1].TrimEnd('/'), CultureInfo.InvariantCulture);
    }

    protected static Task WaitForUrlAsync(IPage page, string url) =>
        page.WaitForURLAsync(url, new PageWaitForURLOptions { WaitUntil = WaitUntilState.Commit });
}
