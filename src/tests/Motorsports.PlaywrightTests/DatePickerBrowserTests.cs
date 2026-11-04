using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class DatePickerBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    private const string FixtureToday = "2026-11-04";

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public Task Picker_opens_as_dialog_that_fits_the_viewport_and_commits_a_chosen_day(string profileName) =>
        RunAsync(
            profileName,
            "date picker dialog",
            async (page, touchProfile) =>
            {
                var date = DateInput(page);
                await Assertions.Expect(date).ToHaveValueAsync(FixtureToday);

                await ActivateAsync(OpenButton(page), touchProfile);
                var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Date" });
                await Assertions.Expect(dialog).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator(".mud-overlay-dialog")).ToBeVisibleAsync();

                var box = await dialog.BoundingBoxAsync();
                var viewport = page.ViewportSize!;
                Assert.NotNull(box);
                Assert.True(box.X >= 0 && box.X + box.Width <= viewport.Width, $"dialog x={box.X} w={box.Width}");
                Assert.True(box.Y >= 0 && box.Y + box.Height <= viewport.Height, $"dialog y={box.Y} h={box.Height}");

                await ActivateAsync(Day(page, "20"), touchProfile);
                await Assertions.Expect(dialog).ToBeHiddenAsync();
                await Assertions.Expect(date).ToHaveValueAsync("2026-11-20");
            }
        );

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public Task Picker_today_action_sets_today_and_escape_keeps_the_value(string profileName) =>
        RunAsync(
            profileName,
            "date picker today and cancel",
            async (page, touchProfile) =>
            {
                var date = DateInput(page);
                var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Date" });

                await ActivateAsync(OpenButton(page), touchProfile);
                await ActivateAsync(Day(page, "20"), touchProfile);
                await Assertions.Expect(date).ToHaveValueAsync("2026-11-20");

                await ActivateAsync(OpenButton(page), touchProfile);
                await ActivateAsync(
                    page.GetByRole(AriaRole.Button, new() { Name = "Today", Exact = true }),
                    touchProfile
                );
                await Assertions.Expect(dialog).ToBeHiddenAsync();
                await Assertions.Expect(date).ToHaveValueAsync(FixtureToday);

                await ActivateAsync(OpenButton(page), touchProfile);
                await Assertions.Expect(dialog).ToBeVisibleAsync();

                // MudBlazor attaches its key handler after the dialog opens; under load the first Escape can be early.
                for (var attempt = 0; attempt < 3 && await dialog.IsVisibleAsync(); attempt++)
                {
                    await page.Keyboard.PressAsync("Escape");
                    await page.WaitForTimeoutAsync(500);
                }

                await Assertions.Expect(dialog).ToBeHiddenAsync();
                await Assertions.Expect(date).ToHaveValueAsync(FixtureToday);
            }
        );

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public Task Mask_builds_the_date_from_typed_digits_and_rejects_other_characters(string profileName) =>
        RunAsync(
            profileName,
            "date mask",
            async (page, touchProfile) =>
            {
                var date = DateInput(page);
                await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).First, touchProfile);
                await Assertions.Expect(date).ToHaveValueAsync("");
                await ActivateAsync(date, touchProfile);

                await date.PressSequentiallyAsync("2027ab08x15", new() { Delay = 60 });
                await Assertions.Expect(date).ToHaveValueAsync("2027-08-15");

                await date.PressSequentiallyAsync("9", new() { Delay = 60 });
                await Assertions.Expect(date).ToHaveValueAsync("2027-08-15");

                await ActivateAsync(OpenButton(page), touchProfile);
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Date" }))
                    .ToContainTextAsync(new Regex("August", RegexOptions.IgnoreCase));
                await ActivateAsync(Day(page, "15"), touchProfile);
                await Assertions.Expect(date).ToHaveValueAsync("2027-08-15");
            }
        );

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public Task Clear_button_empties_the_date(string profileName) =>
        RunAsync(
            profileName,
            "date clear",
            async (page, touchProfile) =>
            {
                var date = DateInput(page);
                await ActivateAsync(page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).First, touchProfile);
                await Assertions.Expect(date).ToHaveValueAsync("");
            }
        );

    private async Task RunAsync(string profileName, string caseName, Func<IPage, bool, Task> body)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var seasonId = await Fixture.CreateSeasonAsync($"PWDP-{Guid.NewGuid():N}"[..12]);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            caseName,
            async () =>
            {
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/seasons/{seasonId}/rounds/create");
                await body(browser.Page, touchProfile);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    private static ILocator DateInput(IPage page) => page.GetByRole(AriaRole.Textbox, new() { Name = "Date*" });

    private static ILocator OpenButton(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Open" }).First;

    private static ILocator Day(IPage page, string day) =>
        page.Locator(".mud-picker-calendar-day:not(.mud-hidden)").GetByText(day, new() { Exact = true });
}
