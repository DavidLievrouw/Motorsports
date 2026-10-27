using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using MySqlConnector;

namespace Motorsports.Infrastructure;

public static class DatabaseConfiguration
{
    // Production is MariaDB 12.3.x. Fixed so startup never needs a connection to detect the version.
    public static readonly ServerVersion Version = ServerVersion.Create(12, 3, 3, ServerType.MariaDb);

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        var connection = new MySqlConnectionStringBuilder(connectionString) { CharacterSet = "utf8mb4" };
        options.UseMySql(connection.ConnectionString, Version, o => o.MigrationsHistoryTable("__EFMigrationsHistory"));
    }

    public static IServiceCollection AddMotorsportsDatabase(
        this IServiceCollection services,
        string connectionString
    ) =>
        services
            .AddPooledDbContextFactory<MotorsportsDbContext>(o => Configure(o, connectionString))
            .AddScoped<MotorsportsDbContext>(sp =>
                sp.GetRequiredService<IDbContextFactory<MotorsportsDbContext>>().CreateDbContext()
            );
}
