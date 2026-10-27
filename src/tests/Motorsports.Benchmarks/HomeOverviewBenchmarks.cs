using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Motorsports.Home;
using Motorsports.Infrastructure;

namespace Motorsports.Benchmarks;

[MemoryDiagnoser]
public class HomeOverviewBenchmarks
{
    private HomeService service = null!;
    private DateOnly today;

    [GlobalSetup]
    public void Setup()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Motorsports");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Motorsports to a populated MariaDB database before running HomeOverviewBenchmarks."
            );
        }

        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, connectionString);
        var factory = new PooledDbContextFactory<MotorsportsDbContext>(options.Options);
        service = new HomeService(factory);
        today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);
    }

    [Benchmark]
    public Task GetHomeOverview() => service.GetHomeOverviewAsync(today);
}
