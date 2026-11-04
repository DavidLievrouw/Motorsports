using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using MySqlConnector;
using Testcontainers.MariaDb;

namespace Motorsports.PlaywrightTests;

public sealed partial class PlaywrightSuiteFixture : IAsyncLifetime
{
    private const string Password = "Synthetic browser password!";
    private readonly MariaDbContainer _database = new MariaDbBuilder("mariadb:12.3.3")
        .WithDatabase("motorsports_playwright")
        .WithUsername("playwright")
        .WithPassword("Synthetic_only_2026!")
        .Build();
    private readonly Dictionary<string, IBrowser> _browsers = new(StringComparer.Ordinal);
    private readonly List<string> _hostOutput = [];
    private readonly object _outputLock = new();
    private IPlaywright? _playwright;
    private Process? _application;
    private string? _repositoryRoot;

    public string BaseUrl { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _repositoryRoot = FindRepositoryRoot();
        _playwright = await Playwright.CreateAsync();
        await _database.StartAsync();
        await CreateSyntheticDatabaseAsync();
        await StartApplicationAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers.Values)
        {
            await browser.CloseAsync();
        }

        _browsers.Clear();
        _playwright?.Dispose();

        if (_application is { HasExited: false })
        {
            _application.Kill(entireProcessTree: true);
            await _application.WaitForExitAsync();
        }

        _application?.Dispose();
        await _database.DisposeAsync();
    }

    public async Task<BrowserSession> OpenBrowserAsync(string profileName)
    {
        var (engine, options) = GetProfile(profileName);
        if (!_browsers.TryGetValue(engine, out var browser))
        {
            var browserType = engine switch
            {
                "chromium" => _playwright!.Chromium,
                "firefox" => _playwright!.Firefox,
                "webkit" => _playwright!.Webkit,
                _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "Unknown browser engine."),
            };
            browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _browsers.Add(engine, browser);
        }

        var context = await browser.NewContextAsync(options);
        await context.Tracing.StartAsync(new TracingStartOptions { Screenshots = false, Snapshots = true });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(15_000);
        return new BrowserSession(profileName, context, page);
    }

    public async Task<string> CreateUserAsync(bool forcePasswordChange = false)
    {
        var username = $"pw-{Guid.NewGuid():N}"[..11];
        await using var context = CreateContext();
        var user = new User
        {
            Id = UserId.From(Guid.NewGuid().ToString()),
            Username = username,
            ForceChangePassword = forcePasswordChange,
        };
        user.PasswordHashV3 = new PasswordHasher<User>().HashPassword(user, Password);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return username;
    }

    public static string TestPassword => Password;

    public async Task ResetThrottleStateAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM `AuthThrottleState` WHERE `Key` <> 'global';");
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE `AuthThrottleState`
            SET `WindowStart` = '2000-01-01',
                `Attempts` = 0,
                `Failures` = 0,
                `BlockedUntil` = '2000-01-01',
                `ExpiresAt` = '2000-01-01',
                `LastFailure` = '2000-01-01',
                `LastAccount` = NULL,
                `AccountChanges` = 0,
                `LastSuccess` = NULL
            WHERE `Key` = 'global';
            """
        );
    }

    private async Task CreateSyntheticDatabaseAsync()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacySchema.sql");
        var schema = await File.ReadAllTextAsync(schemaPath);
        await using (var connection = new MySqlConnection(_database.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using (var disableForeignKeys = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0", connection))
            {
                await disableForeignKeys.ExecuteNonQueryAsync();
            }

            foreach (
                var statement in schema.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            )
            {
                await using var command = new MySqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync();
            }

            await using var enableForeignKeys = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1", connection);
            await enableForeignKeys.ExecuteNonQueryAsync();
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.Countries.Add(
            new Country
            {
                Iso = CountryIso.From("GB"),
                Name = "UNITED KINGDOM",
                NiceName = "United Kingdom",
                Iso3 = "GBR",
            }
        );
        context.Sports.Add(new Sport { Name = SportName.F1, FullName = "Formula One" });
        context.Statuses.AddRange(
            new Status { Name = StatusName.Scheduled, Step = 0 },
            new Status { Name = StatusName.ReadyToWatch, Step = 1 },
            new Status { Name = StatusName.Finished, Step = 2 },
            new Status { Name = StatusName.Stopped, Step = 2 },
            new Status { Name = StatusName.Postponed, Step = 2 },
            new Status { Name = StatusName.Cancelled, Step = 2 }
        );
        context.Venues.Add(new Venue { Name = VenueName.From("Silverstone"), Country = CountryIso.From("GB") });
        await context.SaveChangesAsync();

        var team = new Team
        {
            Name = "Scuderia Ferrari",
            Sport = SportName.F1,
            Country = CountryIso.From("GB"),
        };
        context.Teams.Add(team);
        var season = new Season { Sport = SportName.F1, Label = "2026" };
        context.Seasons.Add(season);
        context.Participants.Add(
            new Participant
            {
                Title = "",
                FirstName = "Alex",
                LastName = "Driver",
                Country = CountryIso.From("GB"),
            }
        );
        await context.SaveChangesAsync();

        context.SeasonEntries.Add(
            new SeasonEntry
            {
                Season = season.Id,
                Team = team.Id,
                Name = "Ferrari",
            }
        );
        context.Rounds.Add(
            new Round
            {
                Date = new DateOnly(2027, 7, 4),
                Number = 1,
                Name = "British Grand Prix",
                Season = season.Id,
                Venue = VenueName.From("Silverstone"),
                Status = StatusName.Scheduled,
            }
        );
        await context.SaveChangesAsync();
        await SeedPickerDataAsync(context);
    }

    private async Task StartApplicationAsync()
    {
        var port = GetAvailablePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        var root = _repositoryRoot!;
        var webProject = Path.Combine(root, "Motorsports.Web", "Motorsports.Web.csproj");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(webProject);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(BaseUrl);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ConnectionStrings__Motorsports"] = _database.GetConnectionString();
        startInfo.Environment["Auth__SecureCookies"] = "false";

        _application = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _application.OutputDataReceived += (_, args) => RecordOutput(args.Data);
        _application.ErrorDataReceived += (_, args) => RecordOutput(args.Data);
        if (!_application.Start())
        {
            throw new InvalidOperationException("The disposable Playwright application process did not start.");
        }

        _application.BeginOutputReadLine();
        _application.BeginErrorReadLine();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        while (!timeout.IsCancellationRequested)
        {
            if (_application.HasExited)
            {
                throw new InvalidOperationException(
                    $"The disposable application exited early.{Environment.NewLine}{HostOutput()}"
                );
            }

            try
            {
                using var response = await client.GetAsync($"{BaseUrl}/health", timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) when (!timeout.IsCancellationRequested) { }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested) { }

            await Task.Delay(100, timeout.Token);
        }

        throw new TimeoutException(
            $"The disposable application did not become ready.{Environment.NewLine}{HostOutput()}"
        );
    }

    private MotorsportsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, _database.GetConnectionString());
        return new MotorsportsDbContext(options.Options);
    }

    private void RecordOutput(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_outputLock)
        {
            _hostOutput.Add(line);
            if (_hostOutput.Count > 100)
            {
                _hostOutput.RemoveAt(0);
            }
        }
    }

    private string HostOutput()
    {
        lock (_outputLock)
        {
            return string.Join(Environment.NewLine, _hostOutput);
        }
    }

    private string FindRepositoryRoot()
    {
        for (
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "Motorsports.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate Motorsports.slnx from the Playwright test output.");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private (string Engine, BrowserNewContextOptions Options) GetProfile(string name) =>
        name switch
        {
            "Chromium desktop" => (
                "chromium",
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 2560, Height = 1440 },
                    DeviceScaleFactor = 1,
                    TimezoneId = "UTC",
                }
            ),
            "Firefox desktop" => (
                "firefox",
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 2560, Height = 1440 },
                    DeviceScaleFactor = 1,
                    TimezoneId = "UTC",
                }
            ),
            "WebKit desktop" => (
                "webkit",
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 2560, Height = 1440 },
                    DeviceScaleFactor = 1,
                    TimezoneId = "UTC",
                }
            ),
            "Pixel 10 Pro Chromium" => (
                "chromium",
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 414, Height = 924 },
                    ScreenSize = new ScreenSize { Width = 414, Height = 924 },
                    DeviceScaleFactor = 1280f / 414f,
                    IsMobile = true,
                    HasTouch = true,
                    UserAgent =
                        "Mozilla/5.0 (Linux; Android 16; Pixel 10 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Mobile Safari/537.36",
                    TimezoneId = "UTC",
                }
            ),
            "iPhone 16 Pro WebKit" => ("webkit", _playwright!.Devices["iPhone 16 Pro"]),
            "360x640 guard" => (
                "chromium",
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 360, Height = 640 },
                    DeviceScaleFactor = 1,
                    TimezoneId = "UTC",
                }
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown Playwright profile."),
        };
}
