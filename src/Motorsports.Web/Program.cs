using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Motorsports.Auth;
using Motorsports.Common;
using Motorsports.Home;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Auth;
using Motorsports.Infrastructure.Lookups;
using Motorsports.Participants;
using Motorsports.Rounds;
using Motorsports.Seasons;
using Motorsports.Sports;
using Motorsports.Teams;
using Motorsports.Venues;
using Motorsports.Web;
using Motorsports.Web.Components;
using MudBlazor.Services;

if (args is ["--health-check"])
{
    var configuredEndpoint = Environment.GetEnvironmentVariable("Kestrel__Endpoints__Http__Url");
    var configuredPorts = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
    var healthPort =
        configuredEndpoint is not null ? new Uri(configuredEndpoint).Port
        : configuredPorts is not null && int.TryParse(configuredPorts, out var port) ? port
        : 8080;
    var healthHost = Environment.GetEnvironmentVariable("HEALTHCHECK_HOST") ?? "ms.dalion.eu";
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{healthPort}/health");
    request.Headers.Host = healthHost;

    try
    {
        using var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
    catch (TaskCanceledException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var connectionString =
    builder.Configuration.GetConnectionString("Motorsports")
    ?? throw new InvalidOperationException("ConnectionStrings:Motorsports is not configured.");

builder.Services.AddMotorsportsDatabase(connectionString);
builder.Services.AddHostedService<DatabaseMigrator>();
builder.Services.AddDataProtection().SetApplicationName("Motorsports").PersistKeysToDbContext<MotorsportsDbContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped(sp => new UserClock(
    sp.GetRequiredService<TimeProvider>(),
    UserClock.ResolveZone(sp.GetRequiredService<IOptions<AppOptions>>().Value.TimeZone)
));
builder.Services.AddSingleton(
    new BuildInfo(
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0"
    )
);

// Auth: Identity over the legacy User table, cookie sign-in, everything requires login unless [AllowAnonymous].
builder.Services.AddAuthServices(builder.Configuration);

// Reference-data slices and the shared lookup cache they invalidate after commits.
builder.Services.Configure<LookupCacheOptions>(builder.Configuration.GetSection(LookupCacheOptions.Section));
builder.Services.AddLookupCache();
builder
    .Services.AddSportsServices()
    .AddVenuesServices()
    .AddTeamsServices()
    .AddParticipantsServices()
    .AddSeasonsServices()
    .AddRoundsServices()
    .AddHomeServices();
builder.Services.AddMotorsportsIdentity().AddSignInManager();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder
    .Services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<IOptions<AuthOptions>>(
        (cookie, auth) =>
        {
            cookie.Cookie.Name = "ms.auth";
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SameSite = SameSiteMode.Lax;
            cookie.Cookie.SecurePolicy = auth.Value.SecureCookies
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            cookie.LoginPath = AuthEndpoints.LoginPath;
            cookie.AccessDeniedPath = AuthEndpoints.AccessDeniedPath;
            cookie.ReturnUrlParameter = "returnUrl";
            cookie.ExpireTimeSpan = TimeSpan.FromDays(7);
            cookie.SlidingExpiration = true;
        }
    );
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));
builder
    .Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

// Configure trusted proxies explicitly so clients cannot spoof X-Forwarded-For to bypass login rate limits.
var knownProxyAddresses = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var knownProxies = knownProxyAddresses
    .Select(proxyAddress =>
    {
        if (!IPAddress.TryParse(proxyAddress, out var address))
        {
            throw new InvalidOperationException(
                $"ForwardedHeaders:KnownProxies contains an invalid IP address: {proxyAddress}"
            );
        }

        return address;
    })
    .ToArray();
builder.Services.PostConfigure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    foreach (var address in knownProxies)
    {
        o.KnownProxies.Add(address);
    }
});

builder.Services.AddMudServices();
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents(o =>
    {
        o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(1);
        o.DisconnectedCircuitMaxRetained = 20;
    });
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment() && knownProxies.Length > 0)
{
    app.UseForwardedHeaders();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(
    async (context, next) =>
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["CspNonce"] = nonce;
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
        context.Response.Headers["Content-Security-Policy-Report-Only"] = string.Join(
            "; ",
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "form-action 'self'",
            $"script-src 'self' 'nonce-{nonce}'",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self' data:",
            "connect-src 'self' ws: wss:"
        );
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        await next();
    }
);
app.UseResponseCompression();
app.UseRouting();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseForcePasswordChange();
app.UseAntiforgery();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapStaticAssets().AllowAnonymous();
app.MapAuthEndpoints();
app.MapSportImages();
app.MapRoundsEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(AuthOptions).Assembly,
        SportsModule.Assembly,
        VenuesModule.Assembly,
        TeamsModule.Assembly,
        ParticipantsModule.Assembly,
        SeasonsModule.Assembly,
        RoundsModule.Assembly,
        HomeModule.Assembly
    )
    .Add(endpoint =>
    {
        if (endpoint is RouteEndpointBuilder route && route.RoutePattern.RawText == "/_framework/blazor.web.js")
        {
            endpoint.Metadata.Add(new AllowAnonymousAttribute());
        }
    });

await app.RunAsync();
return 0;

public partial class Program;
