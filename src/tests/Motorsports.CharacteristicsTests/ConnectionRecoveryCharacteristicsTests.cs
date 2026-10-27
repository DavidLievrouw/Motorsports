using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Home;
using Motorsports.Infrastructure;
using MySqlConnector;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class ConnectionRecoveryCharacteristicsTests(MariaDbFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Healthy_physical_connection_is_reused_between_context_leases()
    {
        var factory = CreateFactory();
        int firstSession;
        await using (var context = await factory.CreateDbContextAsync(Ct))
        {
            await context.Database.OpenConnectionAsync(Ct);
            firstSession = ((MySqlConnection)context.Database.GetDbConnection()).ServerThread;
        }

        await using var reused = await factory.CreateDbContextAsync(Ct);
        await reused.Database.OpenConnectionAsync(Ct);
        ((MySqlConnection)reused.Database.GetDbConnection()).ServerThread.ShouldBe(firstSession);
    }

    [Fact]
    public async Task Server_expired_idle_connection_is_replaced_on_next_checkout()
    {
        var factory = CreateFactory();
        int expiredSession;
        await using (var context = await factory.CreateDbContextAsync(Ct))
        {
            await context.Database.OpenConnectionAsync(Ct);
            expiredSession = ((MySqlConnection)context.Database.GetDbConnection()).ServerThread;
            await context.Database.ExecuteSqlRawAsync("SET SESSION wait_timeout = 1", Ct);
        }

        await Task.Delay(TimeSpan.FromSeconds(3), Ct);

        await using var recovered = await factory.CreateDbContextAsync(Ct);
        await recovered.Database.OpenConnectionAsync(Ct);
        ((MySqlConnection)recovered.Database.GetDbConnection()).ServerThread.ShouldNotBe(expiredSession);
        (await recovered.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync(Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Killed_idle_connection_is_replaced_on_next_checkout()
    {
        var factory = CreateFactory();
        int killedSession;
        await using (var context = await factory.CreateDbContextAsync(Ct))
        {
            await context.Database.OpenConnectionAsync(Ct);
            killedSession = ((MySqlConnection)context.Database.GetDbConnection()).ServerThread;
        }

        await KillConnectionAsync(killedSession);

        await using var recovered = await factory.CreateDbContextAsync(Ct);
        await recovered.Database.OpenConnectionAsync(Ct);
        ((MySqlConnection)recovered.Database.GetDbConnection()).ServerThread.ShouldNotBe(killedSession);
        (await recovered.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync(Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Connection_lost_after_checkout_fails_home_read_but_does_not_poison_the_pool()
    {
        await fixture.ResetSchemaAsync();
        var kill = new KillBeforeRead(KillConnectionAsync);
        var factory = CreateFactory(kill);
        var home = new HomeService(factory);

        await using (var context = await factory.CreateDbContextAsync(Ct))
        {
            context.Database.CreateExecutionStrategy().RetriesOnFailure.ShouldBeFalse();
        }

        var error = await Should.ThrowAsync<Exception>(() => home.GetHomeOverviewAsync(new DateOnly(2026, 10, 27), Ct));
        FindConnectionError(error).ShouldNotBeNull();
        kill.ReadAttempts.ShouldBe(1);

        var recovered = await home.GetHomeOverviewAsync(new DateOnly(2026, 10, 27), Ct);
        recovered.VeryNextUp.ShouldBeNull();
        recovered.NextUp.ShouldBeEmpty();
        recovered.RoundsToAcquire.ShouldBeEmpty();
    }

    private PooledDbContextFactory<MotorsportsDbContext> CreateFactory(params IInterceptor[] interceptors)
    {
        var connection = new MySqlConnectionStringBuilder(fixture.ConnectionString)
        {
            ApplicationName = $"connection-recovery-{Guid.NewGuid():N}",
            MaximumPoolSize = 1,
            MinimumPoolSize = 0,
            ConnectionIdleTimeout = 0,
        };
        connection.ConnectionReset.ShouldBeTrue();
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, connection.ConnectionString);
        options.AddInterceptors(interceptors);
        return new PooledDbContextFactory<MotorsportsDbContext>(options.Options, poolSize: 1);
    }

    private async Task KillConnectionAsync(long session)
    {
        await using var control = new MySqlConnection(
            new MySqlConnectionStringBuilder(fixture.ConnectionString) { Pooling = false }.ConnectionString
        );
        await control.OpenAsync(Ct);
        await using var command = new MySqlCommand($"KILL CONNECTION {session}", control);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static MySqlException? FindConnectionError(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysql)
            {
                return mysql;
            }
        }

        return null;
    }

    private sealed class KillBeforeRead(Func<long, Task> kill) : DbCommandInterceptor
    {
        private int _readAttempts;

        public int ReadAttempts => Volatile.Read(ref _readAttempts);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            if (Interlocked.Increment(ref _readAttempts) == 1)
            {
                await kill(((MySqlConnection)command.Connection!).ServerThread);
            }

            return result;
        }
    }
}
