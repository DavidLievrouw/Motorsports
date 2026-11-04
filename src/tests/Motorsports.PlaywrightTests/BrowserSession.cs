using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Motorsports.PlaywrightTests;

public sealed partial class BrowserSession : IAsyncDisposable
{
    private bool _traceStopped;
    private readonly List<string> _diagnostics = [];
    private readonly List<string> _teardownNoise = [];
    private volatile bool _navigationPending;
    private volatile bool _documentReady;
    private volatile bool _outageExpected;
    private TaskCompletionSource<bool>? _circuitConnection;

    public BrowserSession(string profileName, IBrowserContext context, IPage page)
    {
        ProfileName = profileName;
        Context = context;
        Page = page;
        AttachDiagnostics(Page);
    }

    public IPage Page { get; private set; }
    public IBrowserContext Context { get; }
    public string ProfileName { get; }
    public string DiagnosticText => string.Join(Environment.NewLine, _diagnostics.Concat(_teardownNoise));

    private void AttachDiagnostics(IPage page)
    {
        // Firefox closes the circuit socket while the old document is still alive during a navigation, so the
        // old document reconnects and fails (antiforgery/auth changed). Errors raised between the start of a
        // main-frame navigation and its commit belong to the dying document and are teardown noise.
        page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest && request.Frame.ParentFrame is null)
            {
                _navigationPending = true;
            }
        };
        page.FrameNavigated += (_, frame) =>
        {
            if (frame.ParentFrame is null)
            {
                _navigationPending = false;
            }
        };
        page.PageError += (_, error) =>
        {
            if (_outageExpected && OutageNoise().IsMatch(error.ToString()))
            {
                _teardownNoise.Add($"Ignored expected outage page error: {error}");
                return;
            }

            _diagnostics.Add($"Page error: {error}");
        };
        page.Console += (_, message) =>
        {
            if (message.Type != "error")
            {
                return;
            }

            if (_navigationPending)
            {
                _teardownNoise.Add($"Ignored console error during navigation: {message.Text}");
                return;
            }

            if (_outageExpected && OutageNoise().IsMatch(message.Text))
            {
                _teardownNoise.Add($"Ignored expected outage error: {message.Text}");
                return;
            }

            _diagnostics.Add($"Console error: {message.Text}");
        };
        page.RequestFailed += (_, request) =>
        {
            if (request.Failure is "NS_BINDING_ABORTED" or "Load request cancelled")
            {
                // Firefox cancels requests (superseded navigations, favicon) when a new navigation replaces the
                // document that issued them.
                _teardownNoise.Add($"Ignored aborted request: {request.Method} {request.Url}");
                return;
            }

            if (request.IsNavigationRequest && request.Frame.ParentFrame is null)
            {
                _navigationPending = false;
            }

            _diagnostics.Add($"Request failed: {request.Method} {request.Url} ({request.Failure})");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 500)
            {
                _diagnostics.Add($"HTTP {response.Status}: {response.Url}");
            }

            if (response.Request.IsNavigationRequest && response.Request.Frame.ParentFrame is null && response.Ok)
            {
                _documentReady = true;
            }
        };
        page.WebSocket += (_, socket) =>
        {
            // The old document may open a socket while it unloads; only the new document's socket counts.
            if (_documentReady && new Uri(socket.Url).AbsolutePath == "/_blazor")
            {
                _circuitConnection?.TrySetResult(true);
            }
        };
    }

    public async Task ResetPageAsync()
    {
        await Page.CloseAsync();
        await Context.ClearCookiesAsync();
        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(15_000);
        AttachDiagnostics(Page);
    }

    public Task ArmCircuitWait()
    {
        _documentReady = false;
        _circuitConnection = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _circuitConnection.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    public async Task<IResponse?> NavigateInteractiveAsync(string url)
    {
        var circuitReady = ArmCircuitWait();
        var response = await Page.GotoAsync(url);
        await circuitReady;
        await WaitForDomToSettleAsync();
        return response;
    }

    public async Task ReloadInteractiveAsync()
    {
        var circuitReady = ArmCircuitWait();
        await Page.ReloadAsync();
        await circuitReady;
        await WaitForDomToSettleAsync();
    }

    public Task WaitForDomToSettleAsync() =>
        Page.EvaluateAsync(
            """
            () => new Promise(resolve => {
                let timeout;
                const observer = new MutationObserver(() => {
                    clearTimeout(timeout);
                    timeout = setTimeout(finish, 150);
                });
                const finish = () => {
                    observer.disconnect();
                    resolve();
                };
                observer.observe(document.body, { childList: true, subtree: true, characterData: true });
                timeout = setTimeout(finish, 150);
            })
            """
        );

    public async Task<string> RetainFailureArtifactsAsync()
    {
        var artifactDirectory = Path.Combine(
            Path.GetTempPath(),
            "Motorsports.Playwright",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(artifactDirectory);
        var screenshotPath = Path.Combine(artifactDirectory, "failure.png");
        var tracePath = Path.Combine(artifactDirectory, "trace.zip");

        await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        _traceStopped = true;
        try
        {
            await Page.ScreenshotAsync(
                new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = false,
                    Timeout = 5_000,
                }
            );
        }
        catch (TimeoutException exception)
        {
            _diagnostics.Add($"Failure screenshot was not captured: {exception.Message}");
        }

        return artifactDirectory;
    }

    public void AssertNoBrowserErrors()
    {
        Assert.True(_diagnostics.Count == 0, string.Join(Environment.NewLine, _diagnostics));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_traceStopped)
        {
            await Context.Tracing.StopAsync();
        }

        await Context.CloseAsync();
    }

    public void AddDiagnostic(string diagnostic) => _diagnostics.Add(diagnostic);

    // Tests that deliberately drop the circuit socket expect SignalR's disconnect errors; nothing else is excused.
    public void ExpectCircuitOutages() => _outageExpected = true;

    [GeneratedRegex(
        @"^(\[[^\]]+\] Error: (Connection disconnected with error|Error: WebSocket closed|Error: Cannot send data if the connection is not in the 'Connected' State)|(Unhandled Promise Rejection: )?Error: There was an exception invoking 'DispatchEventAsync'|Popover Service already initialized)"
    )]
    private static partial Regex OutageNoise();
}
