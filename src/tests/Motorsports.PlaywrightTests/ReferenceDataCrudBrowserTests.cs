using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class ReferenceDataCrudBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    private static Task ExpectNativeRequiredBlockAsync(IPage page) =>
        Assertions.Expect(page.Locator("input:invalid").First).ToBeVisibleAsync();

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Venue_list_searches_pages_and_clears(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Venue list search and paging workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/venues");

                var search = page.GetByLabel("Search");
                var rows = page.Locator("tbody tr");
                await search.FillAsync(PickerData.PagingVenuePrefix.ToLowerInvariant());
                await search.PressAsync("Enter");
                await Assertions
                    .Expect(page)
                    .ToHaveURLAsync(new Regex($"[?&]q={PickerData.PagingVenuePrefix}", RegexOptions.IgnoreCase));
                await Assertions.Expect(rows).ToHaveCountAsync(50);
                await Assertions.Expect(rows.First).ToContainTextAsync($"{PickerData.PagingVenuePrefix} 001");

                var pager = page.Locator(".mud-pagination");
                await ActivateAsync(
                    pager.Locator("button").Filter(new() { HasTextRegex = new Regex(@"^\s*2\s*$") }),
                    touchProfile
                );
                await Assertions.Expect(page).ToHaveURLAsync(new Regex("page=2"));
                await Assertions.Expect(rows).ToHaveCountAsync(PickerData.PagingVenueCount - 50);
                await Assertions.Expect(rows.First).ToContainTextAsync($"{PickerData.PagingVenuePrefix} 051");

                await search.FillAsync("Silverstone");
                await search.PressAsync("Enter");
                await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"[?&]q=Silverstone$"));
                await Assertions.Expect(rows).ToHaveCountAsync(1);
                await Assertions.Expect(pager).ToHaveCountAsync(0);

                await ActivateAsync(page.Locator(".mud-input-clear-button"), touchProfile);
                await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("q="));
                await Assertions.Expect(search).ToHaveValueAsync("");
                await Assertions.Expect(pager).ToBeVisibleAsync();
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Venue_create_and_edit_validate_conflict_and_cancel(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);
        var name = $"PW Circuit {Guid.NewGuid():N}"[..20];
        var renamed = $"{name} Renamed";

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Venue create and edit validation workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/venues/create");

                var nameField = page.GetByLabel("Name", new() { Exact = true });
                await SelectAutocompleteAsync(page, "Country", "United", "United Kingdom", touchProfile);
                await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
                await nameField.FillAsync("");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectNativeRequiredBlockAsync(page);

                await nameField.FillAsync(new string('x', 101));
                await Assertions.Expect(nameField).ToHaveValueAsync(new string('x', 100));

                await nameField.FillAsync("silverstone");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "A venue with this name already exists.");

                await nameField.FillAsync(name);
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, $"**/venues/details/{Uri.EscapeDataString(name)}");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/venues/edit/{Uri.EscapeDataString(name)}");
                await nameField.FillAsync("");
                await PressButtonAsync(page, "Save", touchProfile);
                await ExpectNativeRequiredBlockAsync(page);
                await nameField.FillAsync("SILVERSTONE");
                await PressButtonAsync(page, "Save", touchProfile);
                await ExpectTextAsync(page, "A venue with this name already exists.");
                await nameField.FillAsync(renamed);
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/venues/details/{Uri.EscapeDataString(renamed)}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(page.Locator("body")).ToContainTextAsync(renamed);

                await browser.NavigateInteractiveAsync(
                    $"{Fixture.BaseUrl}/venues/edit/{Uri.EscapeDataString(renamed)}"
                );
                await nameField.FillAsync("Discarded venue name");
                await ActivateAsync(
                    page.GetByRole(AriaRole.Link, new() { Name = "Cancel", Exact = true }),
                    touchProfile
                );
                await WaitForUrlAsync(page, $"**/venues/details/{Uri.EscapeDataString(renamed)}");
                await browser.ReloadInteractiveAsync();
                var body = await page.Locator("body").InnerTextAsync();
                Assert.Contains(renamed, body);
                Assert.DoesNotContain("Discarded venue name", body);
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Team_validation_edit_and_delete_guards(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        var disposableTeam = await Fixture.CreateTeamAsync($"PW Delete {Guid.NewGuid():N}"[..20]);
        var keptTeam = await Fixture.CreateTeamAsync($"PW Keep {Guid.NewGuid():N}"[..20]);
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);
        var name = $"PW Team {Guid.NewGuid():N}"[..20];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Team validation, edit, and delete workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/create");

                var nameField = page.GetByLabel("Name", new() { Exact = true });
                await SelectAutocompleteAsync(page, "Sport", "F1", "F1", touchProfile);
                await SelectAutocompleteAsync(page, "Country", "United", "United Kingdom", touchProfile);
                await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
                await nameField.FillAsync("");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectNativeRequiredBlockAsync(page);
                await nameField.FillAsync(new string('x', 51));
                await Assertions.Expect(nameField).ToHaveValueAsync(new string('x', 50));
                await nameField.FillAsync("scuderia ferrari");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "A team with this name already exists in this sport.");
                await nameField.FillAsync(name);
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, "**/teams/details/*");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/edit/{keptTeam}");
                await Assertions.Expect(page.GetByLabel("Sport")).ToBeDisabledAsync();
                await nameField.FillAsync("Scuderia Ferrari");
                await PressButtonAsync(page, "Save", touchProfile);
                await ExpectTextAsync(page, "A team with this name already exists in this sport.");
                await nameField.FillAsync($"{name} edited"[..Math.Min(50, name.Length + 7)]);
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/teams/details/{keptTeam}");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/delete/1");
                await ExpectTextAsync(page, "This team has season entries and cannot be deleted.");
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }))
                    .ToBeDisabledAsync();

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/delete/{keptTeam}");
                await ActivateAsync(
                    page.GetByRole(AriaRole.Link, new() { Name = "Cancel", Exact = true }),
                    touchProfile
                );
                await WaitForUrlAsync(page, $"**/teams/details/{keptTeam}");
                Assert.True(await Fixture.TeamExistsAsync(keptTeam));

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/teams/delete/{disposableTeam}");
                await PressButtonAsync(page, "Delete", touchProfile);
                await WaitForUrlAsync(page, "**/teams");
                await Assertions
                    .Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Teams", Exact = true }))
                    .ToBeVisibleAsync();
                Assert.False(await Fixture.TeamExistsAsync(disposableTeam));
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Participant_validation_duplicates_and_edit(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);
        var first = $"Dup{Guid.NewGuid():N}"[..12];
        var last = $"Lastname{Guid.NewGuid():N}"[..14];

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Participant validation, duplicate, and edit workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/participants/create");

                var firstField = page.GetByLabel("First name", new() { Exact = true });
                var lastField = page.GetByLabel("Last name", new() { Exact = true });
                var titleField = page.GetByLabel("Title", new() { Exact = true });
                await SelectAutocompleteAsync(page, "Country", "United", "United Kingdom", touchProfile);
                await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
                await lastField.FillAsync(last);
                await firstField.FillAsync("");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectNativeRequiredBlockAsync(page);
                await firstField.FillAsync(first);
                await titleField.FillAsync("Eleven char");
                await Assertions.Expect(titleField).ToHaveValueAsync("Eleven cha");
                await titleField.FillAsync("");
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, "**/participants/details/*");
                var participantId = await LastUrlSegmentAsync(page);

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/participants/create");
                await firstField.FillAsync(first.ToUpperInvariant());
                await lastField.FillAsync(last);
                await SelectAutocompleteAsync(page, "Country", "United", "United Kingdom", touchProfile);
                await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, "A participant with the same title, name and country already exists.");
                await SelectAutocompleteAsync(page, "Country", "Nether", "Netherlands", touchProfile);
                await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
                await PressButtonAsync(page, "Create", touchProfile);
                await WaitForUrlAsync(page, "**/participants/details/*");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/participants/edit/{participantId}");
                await lastField.FillAsync("");
                await PressButtonAsync(page, "Save", touchProfile);
                await ExpectNativeRequiredBlockAsync(page);
                await lastField.FillAsync(new string('y', 101));
                await Assertions.Expect(lastField).ToHaveValueAsync(new string('y', 100));
                await lastField.FillAsync($"{last}-Edited");
                await PressButtonAsync(page, "Save", touchProfile);
                await WaitForUrlAsync(page, $"**/participants/details/{participantId}");
                await browser.ReloadInteractiveAsync();
                await Assertions.Expect(page.Locator("body")).ToContainTextAsync($"{last}-Edited");

                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/participants?q=Verstappen");
                var rows = page.Locator("tbody tr");
                await Assertions.Expect(rows).ToHaveCountAsync(1);
                await Assertions.Expect(rows.First).ToContainTextAsync("Verstappen");
                browser.AssertNoBrowserErrors();
            }
        );
    }
}
