using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Motorsports.Infrastructure;

/// <summary>Design-time only (dotnet ef). Reads ConnectionStrings__Motorsports from the environment.</summary>
public sealed class MotorsportsDbContextDesignFactory : IDesignTimeDbContextFactory<MotorsportsDbContext>
{
    public MotorsportsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Motorsports")
            ?? "Server=localhost;Database=motorsports_design;User Id=design;";
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, connectionString);
        return new MotorsportsDbContext(options.Options);
    }
}
