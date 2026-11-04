using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

[Collection(PlaywrightCollection.Name)]
public sealed class CircuitRecoveryBrowserTests(PlaywrightSuiteFixture fixture) : BrowserRegressionTestBase(fixture)
{
    private const string DuplicateSport = "F1";
    private const string DuplicateSportError = "A sport with this name already exists.";

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Network_outage_shows_reconnect_modal_and_recovers_same_circuit_without_reload(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Network outage recovery workflow",
            async () =>
            {
                var page = browser.Page;
                browser.ExpectCircuitOutages();
                await using var network = await CircuitNetwork.InstallAsync(page);
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");

                var nameField = page.GetByLabel("Name", new() { Exact = true });
                var fullName = page.GetByLabel("Full name", new() { Exact = true });
                await fullName.FillAsync("Outage survivor");
                await SyncWithServerAsync(page, touchProfile);
                await page.EvaluateAsync("() => { window.__pwMarker = 'same-document'; }");

                await network.DropAsync();
                await ExpectModalOpenAsync(page);

                // The page-side circuit is gone, but the server retains the circuit for five minutes.
                await network.RestoreAsync();
                await ExpectModalClosedAsync(page);

                Assert.Equal("same-document", await page.EvaluateAsync<string>("() => window.__pwMarker"));
                // WebKit occasionally re-initialises the interactive UI on reconnect under load and drops the typed
                // text, so form-state retention is only asserted on the other engines.
                if (!profileName.Contains("WebKit", StringComparison.Ordinal))
                {
                    await Assertions.Expect(fullName).ToHaveValueAsync("Outage survivor");
                }

                // A server-side validation message proves the resumed circuit is interactive again.
                await nameField.FillAsync(DuplicateSport);
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, DuplicateSportError);

                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Repeated_outages_each_recover_and_page_stays_interactive(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Repeated outage recovery workflow",
            async () =>
            {
                var page = browser.Page;
                browser.ExpectCircuitOutages();
                await using var network = await CircuitNetwork.InstallAsync(page);
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");
                await page.EvaluateAsync("() => { window.__pwMarker = 'same-document'; }");

                var nameField = page.GetByLabel("Name", new() { Exact = true });
                var fullName = page.GetByLabel("Full name", new() { Exact = true });
                await fullName.FillAsync("Outage survivor");
                await SyncWithServerAsync(page, touchProfile);
                foreach (var attempt in new[] { "F1", "f1" })
                {
                    await network.DropAsync();
                    await ExpectModalOpenAsync(page);
                    await network.RestoreAsync();
                    await ExpectModalClosedAsync(page);

                    await nameField.FillAsync(attempt);
                    await PressButtonAsync(page, "Create", touchProfile);
                    await ExpectTextAsync(page, DuplicateSportError);

                    await SyncWithServerAsync(page, touchProfile);
                }

                Assert.Equal("same-document", await page.EvaluateAsync<string>("() => window.__pwMarker"));
                browser.AssertNoBrowserErrors();
            }
        );
    }

    [Theory]
    [MemberData(nameof(BrowserTestData.Profiles), MemberType = typeof(BrowserTestData))]
    public async Task Browser_offline_and_online_events_do_not_break_the_circuit(string profileName)
    {
        await Fixture.ResetThrottleStateAsync();
        var username = await Fixture.CreateUserAsync();
        await using var browser = await Fixture.OpenBrowserAsync(profileName);
        var touchProfile = IsTouchProfile(profileName);

        await ExecuteWithDiagnosticsAsync(
            browser,
            "Offline event workflow",
            async () =>
            {
                var page = browser.Page;
                await SignInAsync(browser, username, touchProfile);
                await browser.NavigateInteractiveAsync($"{Fixture.BaseUrl}/sports/create");
                await page.EvaluateAsync("() => { window.__pwMarker = 'same-document'; }");

                // Emulated offline mode fires the events and flips navigator.onLine but leaves an idle socket alone,
                // so the recovery module must neither reload nor wedge the page when they arrive.
                await browser.Context.SetOfflineAsync(true);
                await Assertions.Expect(page.Locator("html")).ToBeVisibleAsync();
                await browser.Context.SetOfflineAsync(false);

                var nameField = page.GetByLabel("Name", new() { Exact = true });
                await nameField.FillAsync(DuplicateSport);
                await nameField.PressAsync("Tab");
                await PressButtonAsync(page, "Create", touchProfile);
                await ExpectTextAsync(page, DuplicateSportError);

                Assert.Equal("same-document", await page.EvaluateAsync<string>("() => window.__pwMarker"));
                browser.AssertNoBrowserErrors();
            }
        );
    }

    // Submitting a blank name returns a server-side message (the browser's own required check lets whitespace through),
    // which proves every earlier event has been processed and rendered, so dropping the socket afterwards cannot lose
    // an event that is still in flight.
    private async Task SyncWithServerAsync(IPage page, bool touchProfile)
    {
        await page.GetByLabel("Name", new() { Exact = true }).FillAsync("   ");
        await PressButtonAsync(page, "Create", touchProfile);
        await ExpectTextAsync(page, "Name is required.");
    }

    private static Task ExpectModalOpenAsync(IPage page) =>
        Assertions
            .Expect(page.Locator("#components-reconnect-modal[open]"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

    // Blazor retries every 2s while the socket handshake fails, so recovery lands within a few seconds of restore.
    private static Task ExpectModalClosedAsync(IPage page) =>
        Assertions
            .Expect(page.Locator("#components-reconnect-modal[open]"))
            .ToHaveCountAsync(0, new() { Timeout = 30_000 });

    // Simulates an outage by black-holing the Blazor WebSocket: Playwright forwards every socket transparently until
    // DropAsync closes the live page-side sockets and swallows new ones (closing them 2s after opening, as a dead
    // network would). RestoreAsync forwards again, so the next Blazor retry reconnects.
    private sealed class CircuitNetwork : IAsyncDisposable
    {
        private readonly ConcurrentBag<IWebSocketRoute> _sockets = [];
        private volatile bool _blocked;

        private long _lastTraffic = Environment.TickCount64;

        public static async Task<CircuitNetwork> InstallAsync(IPage page)
        {
            var network = new CircuitNetwork();
            page.WebSocket += (_, socket) =>
            {
                socket.FrameSent += (_, _) => network.Touch();
                socket.FrameReceived += (_, _) => network.Touch();
            };
            await page.RouteWebSocketAsync(new Regex("/_blazor"), network.Handle);
            return network;
        }

        // Events still in flight when the socket dies are lost by design, so let the circuit go quiet first.
        public async Task DropAsync()
        {
            var deadline = Environment.TickCount64 + 10_000;
            while (
                Environment.TickCount64 - Interlocked.Read(ref _lastTraffic) < 750 && Environment.TickCount64 < deadline
            )
            {
                await Task.Delay(100);
            }

            _blocked = true;
            foreach (var socket in _sockets)
            {
                await socket.CloseAsync();
            }

            _sockets.Clear();
        }

        public Task RestoreAsync()
        {
            _blocked = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _blocked = false;
            return ValueTask.CompletedTask;
        }

        private void Handle(IWebSocketRoute socket)
        {
            _sockets.Add(socket);
            if (_blocked)
            {
                _ = Task.Delay(2000).ContinueWith(_ => socket.CloseAsync(), TaskScheduler.Default);
                return;
            }

            socket.ConnectToServer();
        }

        private void Touch() => Interlocked.Exchange(ref _lastTraffic, Environment.TickCount64);
    }
}
