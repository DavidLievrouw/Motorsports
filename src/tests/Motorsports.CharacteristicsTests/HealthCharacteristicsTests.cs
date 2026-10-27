using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Infrastructure;
using Motorsports.Web;
using MySqlConnector;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class HealthCharacteristicsTests(MariaDbFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Development", HttpStatusCode.ServiceUnavailable, "unavailable", HttpStatusCode.OK, 0)]
    [InlineData("Production", HttpStatusCode.OK, "Motorsports", HttpStatusCode.OK, 1)]
    [InlineData("Production", HttpStatusCode.ServiceUnavailable, "unavailable", HttpStatusCode.ServiceUnavailable, 1)]
    [InlineData("Production", HttpStatusCode.OK, "wrong application", HttpStatusCode.ServiceUnavailable, 1)]
    [InlineData("Production", HttpStatusCode.Redirect, "Motorsports", HttpStatusCode.ServiceUnavailable, 1)]
    [InlineData("Production", HttpStatusCode.OK, "network-error", HttpStatusCode.ServiceUnavailable, 1)]
    [InlineData("Production", HttpStatusCode.OK, "timeout", HttpStatusCode.ServiceUnavailable, 1)]
    public async Task Health_checks_database_and_public_route_except_in_development(
        string environment,
        HttpStatusCode publicStatus,
        string publicBody,
        HttpStatusCode expectedStatus,
        int expectedRequests
    )
    {
        await fixture.ResetSchemaAsync();
        using var handler = new PublicHandler(publicStatus, publicBody);
        await using var app = new HealthApp(fixture.ConnectionString, environment, handler);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Host = "ms.dalion.eu";

        using var response = await client.GetAsync("/health", Ct);
        response.StatusCode.ShouldBe(expectedStatus);
        handler.Requests.ShouldBe(expectedRequests);
        var registrations = app.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Select(registration => registration.Name).ShouldContain("mariadb");
        registrations.Any(registration => registration.Name == "public-host").ShouldBe(environment != "Development");

        using var live = await client.GetAsync("/health/live", Ct);
        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await live.Content.ReadAsStringAsync(Ct)).ShouldBe("Motorsports");
        handler.Requests.ShouldBe(expectedRequests);
    }

    [Fact]
    public async Task Database_health_rejects_bad_credentials_instead_of_reporting_healthy()
    {
        var connection = new MySqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Password = "deliberately-incorrect-password",
            Pooling = false,
        };
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, connection.ConnectionString);
        var check = new DatabaseHealthCheck(new PooledDbContextFactory<MotorsportsDbContext>(options.Options));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), Ct);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    private sealed class HealthApp(string connectionString, string environment, HttpMessageHandler handler)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Motorsports", connectionString);
            builder.UseSetting("AllowedHosts", "ms.dalion.eu");
            builder.UseSetting("Health:PublicUrl", "https://health.example");
            builder.ConfigureServices(services =>
                services
                    .AddHttpClient(PublicHostHealthCheck.ClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => handler)
            );
        }
    }

    private sealed class PublicHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            request.RequestUri.ShouldBe(new Uri("https://health.example/health/live"));
            if (body == "network-error")
            {
                throw new HttpRequestException("Synthetic network failure.");
            }
            if (body == "timeout")
            {
                throw new TaskCanceledException("Synthetic timeout.");
            }
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
