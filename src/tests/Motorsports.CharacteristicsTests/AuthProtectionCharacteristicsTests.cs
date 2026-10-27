using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Motorsports.Auth;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Auth;
using Motorsports.Infrastructure.Entities;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class AuthProtectionCharacteristicsTests(MariaDbFixture fixture, ITestOutputHelper output)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero).AddTicks(7);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now += amount;
    }

    private AuthThrottleStore Store(Clock clock, AuthProtectionOptions? options = null) =>
        new(
            fixture.CreateContextFactory(),
            Options.Create(options ?? new AuthProtectionOptions()),
            clock,
            NullLogger<AuthThrottleStore>.Instance
        );

    private async Task InitializeAsync()
    {
        await fixture.ResetSchemaAsync();
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        await db.Database.MigrateAsync(Ct);
    }

    private static async Task<AuthAdmission> ReserveAsync(
        AuthThrottleStore store,
        string account = "UNKNOWN",
        string ip = "203.0.113.1",
        string device = "device"
    )
    {
        var result = await store.ReserveAsync(account, ip, device, Ct);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    [Fact]
    public void Configuration_rejects_rates_that_would_outgrow_bounded_cleanup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Auth:Protection:GlobalAttempts"] = "1000",
                    ["Auth:Protection:GlobalWindowSeconds"] = "1",
                }
            )
            .Build();
        using var services = new ServiceCollection().AddLogging().AddAuthServices(configuration).BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() =>
        {
            _ = services.GetRequiredService<IOptions<AuthProtectionOptions>>().Value;
        });
    }

    [Fact]
    public async Task Account_budget_is_atomic_across_instances_and_survives_store_recreation()
    {
        await InitializeAsync();
        var clock = new Clock();
        var stores = new[] { Store(clock), Store(clock) };
        var admissions = await Task.WhenAll(
            Enumerable
                .Range(0, 12)
                .Select(index => ReserveAsync(stores[index % 2], ip: $"203.0.113.{index}", device: $"device-{index}"))
        );

        admissions.Count(result => result.Reservation is not null).ShouldBe(5);
        admissions.Where(result => result.Reservation is null).ShouldAllBe(result => result.RetryAfterSeconds == 900);
        (await ReserveAsync(Store(clock), ip: "different", device: "different")).Reservation.ShouldBeNull();
        clock.Advance(TimeSpan.FromMinutes(15));
        (await ReserveAsync(Store(clock), ip: "different", device: "different")).Reservation.ShouldNotBeNull();
    }

    [Fact]
    public async Task Global_budget_is_shared_atomic_and_rejected_requests_do_not_create_keys()
    {
        await InitializeAsync();
        var clock = new Clock();
        var options = new AuthProtectionOptions { GlobalAttempts = 6 };
        var stores = new[] { Store(clock, options), Store(clock, options) };
        var admissions = await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(index => ReserveAsync(stores[index % 2], $"USER-{index}", $"ip-{index}", $"device-{index}"))
        );
        admissions.Count(result => result.Reservation is not null).ShouldBe(6);
        admissions.Where(result => result.Reservation is null).ShouldAllBe(result => result.RetryAfterSeconds == 60);
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(19);
        clock.Advance(TimeSpan.FromMinutes(1));
        (await ReserveAsync(Store(clock, options), "new")).Reservation.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(true, 20)]
    [InlineData(false, 10)]
    public async Task Source_budgets_survive_rotating_other_identifiers(bool sameIp, int permits)
    {
        await InitializeAsync();
        var store = Store(new Clock());
        for (var index = 0; index < permits; index++)
        {
            (
                await ReserveAsync(
                    store,
                    $"USER-{index}",
                    sameIp ? "shared-ip" : $"ip-{index}",
                    sameIp ? $"device-{index}" : "shared-device"
                )
            ).Reservation.ShouldNotBeNull();
        }
        var blocked = await ReserveAsync(
            store,
            "another",
            sameIp ? "shared-ip" : "another-ip",
            sameIp ? "another-device" : "shared-device"
        );
        blocked.Reservation.ShouldBeNull();
        blocked.RetryAfterSeconds.ShouldBe(300);
    }

    [Fact]
    public async Task Existing_account_aliases_use_the_same_budget_and_unknown_names_follow_the_same_policy()
    {
        await InitializeAsync();
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        db.Users.Add(new User { Id = UserId.From("synthetic-id"), Username = "Synthetïc" });
        await db.SaveChangesAsync(Ct);
        var store = Store(new Clock());
        string[] aliases = ["SYNTHETIC", "synthetïc", "synthetic ", "SYNTHETÏC", "Synthetic"];
        foreach (var alias in aliases)
        {
            var admitted = await ReserveAsync(store, alias, alias, alias);
            admitted.Reservation.ShouldNotBeNull();
            admitted.Reservation.Keys[0].ShouldBe(AuthThrottleStore.Key("account", "id:synthetic-id"));
        }
        var known = await ReserveAsync(store, "synthetic", "new-ip", "new-device");
        for (var index = 0; index < 5; index++)
        {
            (
                await ReserveAsync(store, "UNKNOWN", $"unknown-ip-{index}", $"unknown-device-{index}")
            ).Reservation.ShouldNotBeNull();
        }
        var unknown = await ReserveAsync(store, "UNKNOWN", "fresh-ip", "fresh-device");
        known.Reservation.ShouldBeNull();
        unknown.Reservation.ShouldBeNull();
        unknown.RetryAfterSeconds.ShouldBe(known.RetryAfterSeconds);
    }

    [Fact]
    public async Task Failure_cooldowns_double_cap_and_decay_without_sleeping_requests()
    {
        await InitializeAsync();
        var clock = new Clock();
        var store = Store(clock, new AuthProtectionOptions { AccountFailures = 100, DeviceAttempts = 100 });
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            var admitted = await ReserveAsync(store);
            admitted.Reservation.ShouldNotBeNull();
            (await store.CompleteAsync(admitted.Reservation, AuthAttemptOutcome.Failed, Ct)).IsSuccess.ShouldBeTrue();
            var next = await ReserveAsync(store);
            if (attempt < 3)
            {
                // This probe reserved another attempt; neutral completion does not earn device trust.
                next.Reservation.ShouldNotBeNull();
                await store.CompleteAsync(next.Reservation, AuthAttemptOutcome.InvalidInput, Ct);
                continue;
            }
            next.Reservation.ShouldBeNull();
            var expected = Math.Min(60, 1 << (attempt - 3));
            next.RetryAfterSeconds.ShouldBe(expected);
            clock.Advance(TimeSpan.FromSeconds(expected));
        }
        clock.Advance(TimeSpan.FromMinutes(15));
        var afterQuiet = await ReserveAsync(store);
        await store.CompleteAsync(afterQuiet.Reservation!, AuthAttemptOutcome.Failed, Ct);
        (await ReserveAsync(store)).Reservation.ShouldNotBeNull();
    }

    [Fact]
    public async Task Successful_signins_release_only_their_own_account_reservation()
    {
        await InitializeAsync();
        var store = Store(new Clock());
        var first = await ReserveAsync(store);
        var second = await ReserveAsync(store);
        await store.CompleteAsync(first.Reservation!, AuthAttemptOutcome.Failed, Ct);
        await store.CompleteAsync(second.Reservation!, AuthAttemptOutcome.Succeeded, Ct);
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        var account = await db.AuthThrottleStates.SingleAsync(row => row.Key == first.Reservation!.Keys[0], Ct);
        account.Attempts.ShouldBe(1);
        account.Failures.ShouldBe(1);
        var device = await db.AuthThrottleStates.SingleAsync(row => row.Key == first.Reservation!.Keys[2], Ct);
        device.Attempts.ShouldBe(2);
        device.LastSuccess.ShouldNotBeNull();
        device.Failures.ShouldBe(1);
    }

    [Fact]
    public async Task Password_spraying_increases_source_penalties_and_reputation_is_local()
    {
        await InitializeAsync();
        var clock = new Clock();
        var store = Store(clock);
        for (var index = 0; index < 5; index++)
        {
            var admitted = await ReserveAsync(store, $"TARGET-{index}");
            admitted.Reservation.ShouldNotBeNull();
            await store.CompleteAsync(admitted.Reservation, AuthAttemptOutcome.Failed, Ct);
            if (index >= 2 && index < 4)
            {
                clock.Advance(TimeSpan.FromSeconds(1 << (index - 2)));
            }
        }
        var blocked = await ReserveAsync(store, "ANOTHER-TARGET");
        blocked.Reservation.ShouldBeNull();
        blocked.RetryAfterSeconds.ShouldBe(16);
        (await ReserveAsync(store, "ANOTHER-TARGET", "clean-ip", "clean-device")).Reservation.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Recent_success_defers_source_cooldowns_by_one_failure(bool recognized)
    {
        await InitializeAsync();
        var store = Store(new Clock());
        if (recognized)
        {
            var successful = await ReserveAsync(store);
            await store.CompleteAsync(successful.Reservation!, AuthAttemptOutcome.Succeeded, Ct);
        }
        for (var index = 0; index < 3; index++)
        {
            var admitted = await ReserveAsync(store, $"TARGET-{index}");
            admitted.Reservation.ShouldNotBeNull();
            await store.CompleteAsync(admitted.Reservation, AuthAttemptOutcome.Failed, Ct);
        }
        var next = await ReserveAsync(store, "NEW-TARGET");
        (next.Reservation is not null).ShouldBe(recognized);
        next.RetryAfterSeconds.ShouldBe(recognized ? 0 : 1);
    }

    [Fact]
    public async Task Cleanup_expires_reputation_in_indexed_bounded_batches_and_operator_reset_is_safe()
    {
        await InitializeAsync();
        var clock = new Clock();
        var store = Store(clock);
        var admitted = await ReserveAsync(store);
        await store.CompleteAsync(admitted.Reservation!, AuthAttemptOutcome.Failed, Ct);
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        var initial = clock.GetUtcNow().UtcDateTime;
        for (var index = 0; index < 1000; index++)
        {
            db.AuthThrottleStates.Add(
                new AuthThrottleState
                {
                    Key = $"synthetic:{index}",
                    ExpiresAt = index < 600 ? initial.AddHours(1) : initial.AddHours(48),
                }
            );
        }
        await db.SaveChangesAsync(Ct);
        clock.Advance(TimeSpan.FromHours(24));
        (await store.CleanupAsync(Ct)).IsSuccess.ShouldBeTrue();
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(504);
        clock.Advance(TimeSpan.FromMinutes(1));
        (await store.CleanupAsync(Ct)).IsSuccess.ShouldBeTrue();
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(401);
        clock.Advance(TimeSpan.FromHours(24));
        (await store.CleanupAsync(Ct)).IsSuccess.ShouldBeTrue();
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(1);
        var indexes = await db
            .Database.SqlQueryRaw<string>(
                """
                SELECT DISTINCT INDEX_NAME AS Value FROM information_schema.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AuthThrottleState'
                """
            )
            .ToArrayAsync(Ct);
        indexes.ShouldContain("PRIMARY");
        indexes.ShouldContain("IX_AuthThrottleState_ExpiresAt");

        var next = await ReserveAsync(store);
        await db.AuthThrottleStates.Where(row => row.Key != "global").ExecuteDeleteAsync(Ct);
        (await store.CompleteAsync(next.Reservation!, AuthAttemptOutcome.Succeeded, Ct)).IsSuccess.ShouldBeFalse();
        (await ReserveAsync(store)).Reservation.ShouldNotBeNull();
    }

    [Fact]
    public async Task Steady_auth_operations_have_bounded_queries_and_use_key_and_expiry_indexes()
    {
        await InitializeAsync();
        var counter = new CommandCounter();
        var dbOptions = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(dbOptions, fixture.ConnectionString);
        dbOptions.AddInterceptors(counter);
        var factory = new PooledDbContextFactory<MotorsportsDbContext>(dbOptions.Options);
        var store = new AuthThrottleStore(
            factory,
            Options.Create(
                new AuthProtectionOptions
                {
                    GlobalAttempts = 21,
                    IpAttempts = 100,
                    DeviceAttempts = 100,
                }
            ),
            new Clock(),
            NullLogger<AuthThrottleStore>.Instance
        );
        var warmup = await ReserveAsync(store);
        await store.CompleteAsync(warmup.Reservation!, AuthAttemptOutcome.Succeeded, Ct);
        counter.Count = 0;
        var timings = new double[20];
        for (var index = 0; index < timings.Length; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var admission = await ReserveAsync(store);
            admission.Reservation.ShouldNotBeNull();
            (
                await store.CompleteAsync(admission.Reservation, AuthAttemptOutcome.Succeeded, Ct)
            ).IsSuccess.ShouldBeTrue();
            timings[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        counter.Count.ShouldBe(140);
        counter.Count = 0;
        (await ReserveAsync(store)).Reservation.ShouldBeNull();
        counter.Count.ShouldBe(1);
        Array.Sort(timings);
        output.WriteLine(
            $"Synthetic MariaDB reserve+complete: p50={timings[9]:F2} ms, p95={timings[18]:F2} ms; 7 SQL commands/pair, 1/global rejection."
        );

        await using var db = await factory.CreateDbContextAsync(Ct);
        var keyPlan = await db
            .Database.SqlQueryRaw<QueryPlan>(
                "EXPLAIN SELECT * FROM `AuthThrottleState` WHERE `Key` = 'global' FOR UPDATE"
            )
            .ToArrayAsync(Ct);
        keyPlan.Single().Index.ShouldBe("PRIMARY");
        var expiryPlan = await db
            .Database.SqlQueryRaw<QueryPlan>(
                """
                EXPLAIN SELECT `Key` FROM `AuthThrottleState`
                WHERE `Key` <> 'global' AND `ExpiresAt` <= '2027-01-01'
                ORDER BY `ExpiresAt` LIMIT 500
                """
            )
            .ToArrayAsync(Ct);
        expiryPlan.Single().Index.ShouldBe("IX_AuthThrottleState_ExpiresAt");
    }

    private sealed record QueryPlan([property: Column("key")] string? Index);

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            Count++;
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task Missing_protection_storage_fails_closed()
    {
        await InitializeAsync();
        await fixture.ExecuteSqlAsync("DROP TABLE `AuthThrottleState`");
        var result = await Store(new Clock()).ReserveAsync("UNKNOWN", "ip", "device", Ct);
        result.IsSuccess.ShouldBeFalse();
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Migration_is_guarded_reentrant_and_supports_down_and_reapply()
    {
        await InitializeAsync();
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
        await fixture.ExecuteSqlAsync(
            "DELETE FROM `__EFMigrationsHistory` WHERE MigrationId = '20261027124050_AddAuthProtection'"
        );
        await db.Database.MigrateAsync(Ct);
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(1);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261027074827_RemoveLegacyPasswordColumns", Ct);
        await migrator.MigrateAsync(null, Ct);
        (await db.AuthThrottleStates.CountAsync(Ct)).ShouldBe(1);
    }
}
