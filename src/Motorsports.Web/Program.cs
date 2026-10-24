using System.Reflection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Web;
using Motorsports.Web.Components;
using MudBlazor.Services;

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

app.UseResponseCompression();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();

public partial class Program;
