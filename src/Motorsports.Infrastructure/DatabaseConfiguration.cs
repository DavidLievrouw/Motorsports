using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Motorsports.Infrastructure;

public static class DatabaseConfiguration
{
    // Production is MariaDB 11.7.x. Fixed so startup never needs a connection to detect the version.
    public static readonly ServerVersion Version = ServerVersion.Create(11, 7, 2, ServerType.MariaDb);

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        var connection = new MySqlConnectionStringBuilder(connectionString) { CharacterSet = "utf8mb4" };
        options.UseMySql(connection.ConnectionString, Version, o => o.MigrationsHistoryTable("__EFMigrationsHistory"));
    }

    public static IServiceCollection AddMotorsportsDatabase(
        this IServiceCollection services,
        string connectionString
    ) => services.AddPooledDbContextFactory<MotorsportsDbContext>(o => Configure(o, connectionString));
}
