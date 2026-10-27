using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Motorsports.Infrastructure;
using MySqlConnector;
using Testcontainers.MariaDb;

namespace Motorsports.CharacteristicsTests.Fixtures;

public sealed class MariaDbFixture : IAsyncLifetime
{
    private readonly MariaDbContainer container = new MariaDbBuilder("mariadb:11.7.2")
        .WithDatabase("motorsports_test")
        .WithUsername("test")
        .WithPassword("Motorsports_test_123!")
        .Build();

    private readonly string schema = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacySchema.sql")
    );

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    public async Task ResetSchemaAsync()
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, "SET FOREIGN_KEY_CHECKS = 0");
        await ExecuteAsync(connection, "DROP TABLE IF EXISTS `DataProtectionKeys`");
        await ExecuteAsync(connection, "DROP TABLE IF EXISTS `__EFMigrationsHistory`");
        await ExecuteBatchAsync(connection, schema);
        await ExecuteAsync(connection, "SET FOREIGN_KEY_CHECKS = 1");
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await ExecuteBatchAsync(connection, sql);
    }

    public async Task RestoreBackupWithoutKeyRingAsync(CancellationToken cancellationToken)
    {
        var result = await container.ExecAsync(
            [
                "sh",
                "-c",
                """
                set -eu
                backup=$(mktemp)
                trap 'rm -f "$backup"' EXIT
                export MYSQL_PWD="$MARIADB_PASSWORD"
                mariadb-dump --user="$MARIADB_USER" --single-transaction --routines --triggers --hex-blob \
                    --default-character-set=utf8mb4 --ignore-table="$MARIADB_DATABASE.DataProtectionKeys" \
                    "$MARIADB_DATABASE" > "$backup"
                mariadb-dump --user="$MARIADB_USER" --single-transaction --default-character-set=utf8mb4 \
                    --no-data "$MARIADB_DATABASE" DataProtectionKeys >> "$backup"
                mariadb --user="$MARIADB_USER" --execute="DROP DATABASE \`$MARIADB_DATABASE\`; CREATE DATABASE \`$MARIADB_DATABASE\`;"
                mariadb --user="$MARIADB_USER" --default-character-set=utf8mb4 "$MARIADB_DATABASE" < "$backup"
                """,
            ],
            cancellationToken
        );
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Synthetic database backup/restore failed: {result.Stderr}");
        }
    }

    public IDbContextFactory<MotorsportsDbContext> CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, ConnectionString);
        return new PooledDbContextFactory<MotorsportsDbContext>(options.Options);
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteBatchAsync(MySqlConnection connection, string sql)
    {
        foreach (
            var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        )
        {
            await ExecuteAsync(connection, statement);
        }
    }
}
