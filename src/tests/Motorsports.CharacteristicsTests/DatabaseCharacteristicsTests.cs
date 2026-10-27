using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using MySqlConnector;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[CollectionDefinition("MariaDB", DisableParallelization = true)]
public sealed class MariaDbCollection : ICollectionFixture<MariaDbFixture>
{
    public const string Name = "MariaDB";
}

[Collection(MariaDbCollection.Name)]
public sealed class DatabaseCharacteristicsTests(MariaDbFixture fixture)
{
    [Fact]
    public async Task Migrator_applies_all_migrations_once_and_is_idempotent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        var migrator = CreateMigrator(factory);

        await migrator.MigrateAsync(cancellationToken);
        await migrator.MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        appliedMigrations.ShouldBe([
            "20261024151208_Baseline",
            "20261024151224_AddUserAuthColumns",
            "20261024163411_AddSportImages",
            "20261025083000_ParticipantUniqueIdentity",
            "20261026081111_PersistDataProtectionKeys",
            "20261026100239_AddBusinessUniqueConstraints",
            "20261026133143_AddRoundSeasonDateNumberIndex",
            "20261027074827_RemoveLegacyPasswordColumns",
        ]);
        (await context.Users.CountAsync(cancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Legacy_password_removal_preserves_identity_accounts_and_supports_down_and_reapply()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await using var context = await fixture.CreateContextFactory().CreateDbContextAsync(cancellationToken);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026133143_AddRoundSeasonDateNumberIndex", cancellationToken);
        var user = new User { Id = UserId.From("identity-user"), Username = "identity-user" };
        var hash = new PasswordHasher<User>().HashPassword(user, "Synthetic password!");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO `User` (`Id`, `Username`, `PasswordHash`, `Salt`, `Iterations`, `Prf`,
                                `PasswordHashV3`, `SecurityStamp`, `ConcurrencyStamp`, `ForceChangePassword`)
            VALUES ('identity-user', 'identity-user', X'01', X'02', 1000, 'HMACSHA512',
                    {hash}, 'security-stamp', 'concurrency-stamp', 1)
            """,
            cancellationToken
        );

        await migrator.MigrateAsync(null, cancellationToken);
        (await LegacyPasswordColumnCountAsync(context, cancellationToken)).ShouldBe(0);
        var saved = await context.Users.AsNoTracking().SingleAsync(cancellationToken);
        saved.Id.ShouldBe(user.Id);
        (saved.PasswordHashV3 == hash).ShouldBeTrue();
        saved.SecurityStamp.ShouldBe("security-stamp");
        saved.ConcurrencyStamp.ShouldBe("concurrency-stamp");
        saved.ForceChangePassword.ShouldBeTrue();
        saved.IsDeleted.ShouldBeFalse();
        new PasswordHasher<User>()
            .VerifyHashedPassword(saved, saved.PasswordHashV3!, "Synthetic password!")
            .ShouldBe(PasswordVerificationResult.Success);

        await migrator.MigrateAsync("20261026133143_AddRoundSeasonDateNumberIndex", cancellationToken);
        (await LegacyPasswordColumnCountAsync(context, cancellationToken)).ShouldBe(4);
        await migrator.MigrateAsync(null, cancellationToken);
        (await LegacyPasswordColumnCountAsync(context, cancellationToken)).ShouldBe(0);
        await CreateMigrator(fixture.CreateContextFactory()).MigrateAsync(cancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Legacy_password_removal_refuses_to_lock_out_active_accounts(string? hash)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await using var context = await fixture.CreateContextFactory().CreateDbContextAsync(cancellationToken);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026133143_AddRoundSeasonDateNumberIndex", cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO `User` (`Id`, `Username`, `PasswordHash`, `Salt`, `Iterations`, `Prf`, `PasswordHashV3`)
            VALUES ('unconverted-user', 'unconverted-user', X'01', X'02', 1000, 'HMACSHA512', {hash})
            """,
            cancellationToken
        );

        var error = await Should.ThrowAsync<MySqlException>(() => migrator.MigrateAsync(null, cancellationToken));

        error.Message.ShouldContain("Active users need Identity password hashes");
        (await LegacyPasswordColumnCountAsync(context, cancellationToken)).ShouldBe(4);
        (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ShouldContain(
            "20261027074827_RemoveLegacyPasswordColumns"
        );
    }

    [Fact]
    public async Task Legacy_password_removal_accepts_preexisting_removal_and_deleted_accounts_without_passwords()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await using var context = await fixture.CreateContextFactory().CreateDbContextAsync(cancellationToken);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026133143_AddRoundSeasonDateNumberIndex", cancellationToken);
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `User` (`Id`, `Username`, `PasswordHash`, `Salt`, `Iterations`, `Prf`, `IsDeleted`)
            VALUES ('deleted-user', 'deleted-user', X'01', X'02', 1000, 'HMACSHA512', 1);
            ALTER TABLE `User` DROP COLUMN `PasswordHash`, DROP COLUMN `Salt`, DROP COLUMN `Iterations`, DROP COLUMN `Prf`;
            """
        );

        await migrator.MigrateAsync(null, cancellationToken);

        (await LegacyPasswordColumnCountAsync(context, cancellationToken)).ShouldBe(0);
        var user = await context.Users.AsNoTracking().SingleAsync(cancellationToken);
        user.IsDeleted.ShouldBeTrue();
        user.PasswordHashV3.ShouldBeNull();
    }

    private static Task<int> LegacyPasswordColumnCountAsync(
        MotorsportsDbContext context,
        CancellationToken cancellationToken
    ) =>
        context
            .Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS `Value` FROM information_schema.columns
                WHERE table_schema = DATABASE() AND table_name = 'User'
                  AND column_name IN ('PasswordHash', 'Salt', 'Iterations', 'Prf')
                """
            )
            .SingleAsync(cancellationToken);

    [Fact]
    public async Task Data_protection_key_migration_persists_keys_in_the_application_database()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using (var context = await factory.CreateDbContextAsync(cancellationToken))
        {
            context.DataProtectionKeys.Add(
                new Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey
                {
                    FriendlyName = "synthetic-key",
                    Xml = "<key />",
                }
            );
            await context.SaveChangesAsync(cancellationToken);
        }

        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var verified = await factory.CreateDbContextAsync(cancellationToken);
        var key = await verified.DataProtectionKeys.AsNoTracking().SingleAsync(cancellationToken);
        key.FriendlyName.ShouldBe("synthetic-key");
        key.Xml.ShouldBe("<key />");
    }

    [Fact]
    public async Task Participant_identity_migration_only_adds_unique_index()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES ('GB', 'UNITED KINGDOM', 'United Kingdom');
            INSERT INTO `Sport` (`Name`) VALUES ('F1');
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Finished', 20);
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Test Circuit', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES ('F1', '2025'), ('F1', '2024');
            INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`)
            VALUES ('', ' Alex ', 'Driver', 'GB'), ('', 'Sam', 'Driver', 'GB');
            INSERT INTO `Round` (`Date`, `Number`, `Season`, `Venue`, `Status`)
            VALUES ('2025-01-01', 1, 1, 'Test Circuit', 'Finished'),
                   ('2024-01-01', 1, 2, 'Test Circuit', 'Finished');
            INSERT INTO `RoundWinner` (`Round`, `Participant`) VALUES (1, 1), (2, 2);
            INSERT INTO `SeasonWinner` (`Season`, `Participant`) VALUES (1, 1), (2, 2);
            """
        );

        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var participants = await context.Participants.AsNoTracking().ToArrayAsync(cancellationToken);
        participants
            .Select(participant => (participant.Id.Value, participant.FirstName))
            .ShouldBe([(1, " Alex "), (2, "Sam")]);
        var roundWinners = await context
            .RoundWinners.AsNoTracking()
            .OrderBy(winner => winner.Round)
            .Select(winner => new { Round = winner.Round.Value, Participant = winner.Participant.Value })
            .ToArrayAsync(cancellationToken);
        roundWinners.Select(winner => (winner.Round, winner.Participant)).ShouldBe([(1, 1), (2, 2)]);
        var seasonWinners = await context
            .SeasonWinners.AsNoTracking()
            .OrderBy(winner => winner.Season)
            .Select(winner => new { Season = winner.Season.Value, Participant = winner.Participant.Value })
            .ToArrayAsync(cancellationToken);
        seasonWinners.Select(winner => (winner.Season, winner.Participant)).ShouldBe([(1, 1), (2, 2)]);
        (
            await context
                .Database.SqlQueryRaw<long>(
                    "SELECT COUNT(DISTINCT index_name) AS `Value` FROM information_schema.statistics "
                        + "WHERE table_schema = DATABASE() AND table_name = 'Participant' "
                        + "AND index_name = 'IX_Participant_Title_FirstName_LastName_Country' AND non_unique = 0"
                )
                .SingleAsync(cancellationToken)
        ).ShouldBe(1);
    }

    [Fact]
    public async Task Business_constraints_allow_unnumbered_rounds_and_enforce_positive_uniqueness()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES ('GB', 'UNITED KINGDOM', 'United Kingdom');
            INSERT INTO `Sport` (`Name`) VALUES ('F1'), ('MotoGP');
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Scheduled', 0);
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Test Circuit', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES ('F1', '2025'), ('F1', '2026');
            """
        );

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.Teams.AddRange(
            new Team
            {
                Name = "Same name",
                Sport = SportName.From("F1"),
                Country = CountryIso.From("GB"),
            },
            new Team
            {
                Name = "Same name",
                Sport = SportName.From("MotoGP"),
                Country = CountryIso.From("GB"),
            }
        );
        await context.SaveChangesAsync(cancellationToken);

        context.Teams.Add(
            new Team
            {
                Name = "Same name",
                Sport = SportName.From("F1"),
                Country = CountryIso.From("GB"),
            }
        );
        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));
        context.ChangeTracker.Clear();

        context.Rounds.AddRange(CreateRound(0, 1), CreateRound(0, 1), CreateRound(1, 1), CreateRound(1, 2));
        await context.SaveChangesAsync(cancellationToken);

        context.Rounds.Add(CreateRound(1, 1));
        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));

        Round CreateRound(short number, int seasonId) =>
            new()
            {
                Date = new DateOnly(2026, 1, number + 1),
                Number = number,
                Season = SeasonId.From(seasonId),
                Venue = VenueName.From("Test Circuit"),
                Status = StatusName.Scheduled,
            };
    }

    [Fact]
    public async Task Sport_image_migration_seeds_the_current_legacy_logos()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Sport` (`Name`) VALUES ('f1'),('fe'),('indycar'),('motogp'),('nascar'),('wec')"
        );

        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var images = await context
            .Sports.AsNoTracking()
            .ToDictionaryAsync(sport => sport.Name, sport => sport.Image, cancellationToken);
        foreach (var name in new[] { "f1", "fe", "indycar", "motogp", "nascar", "wec" })
        {
            await using var stream = typeof(MotorsportsDbContext).Assembly.GetManifestResourceStream(
                $"Motorsports.Infrastructure.Migrations.SportImages.{name}.png"
            )!;
            using var expected = new MemoryStream();
            await stream.CopyToAsync(expected, cancellationToken);
            images[SportName.From(name)].ShouldBe(expected.ToArray());
        }
    }

    [Fact]
    public async Task Business_constraint_migration_can_be_reverted_and_reapplied()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026081111_PersistDataProtectionKeys", cancellationToken);
        await migrator.MigrateAsync(null, cancellationToken);
    }

    [Fact]
    public async Task Round_history_index_accepts_existing_schema_and_preserves_rounds_on_down_and_reapply()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES ('GB', 'UNITED KINGDOM', 'United Kingdom');
            INSERT INTO `Sport` (`Name`) VALUES ('F1');
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Scheduled', 0);
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Synthetic circuit', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES ('F1', 'Synthetic season');
            INSERT INTO `Round` (`Date`, `Number`, `Season`, `Venue`, `Status`)
            VALUES ('2026-01-01', 1, 1, 'Synthetic circuit', 'Scheduled'),
                   ('2026-01-01', 2, 1, 'Synthetic circuit', 'Scheduled'),
                   ('2026-02-01', 0, 1, 'Synthetic circuit', 'Scheduled'),
                   ('2026-02-01', 0, 1, 'Synthetic circuit', 'Scheduled');
            CREATE INDEX `IX_Round_Season_Date_Number` ON `Round` (`Season`, `Date`, `Number`);
            """
        );
        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var expected = await ReadRoundsAsync();
        expected.Select(round => round.Id).ShouldBe([1, 2, 3, 4]);
        expected
            .Select(round => (round.Date, round.Number))
            .ShouldBe([
                (new DateOnly(2026, 1, 1), (short)1),
                (new DateOnly(2026, 1, 1), (short)2),
                (new DateOnly(2026, 2, 1), (short)0),
                (new DateOnly(2026, 2, 1), (short)0),
            ]);
        expected.ShouldAllBe(round =>
            round.Season == 1 && round.Venue == "Synthetic circuit" && round.Status == "Scheduled"
        );
        (await IndexColumnsAsync()).ShouldBe(["Season", "Date", "Number"]);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026100239_AddBusinessUniqueConstraints", cancellationToken);
        (await IndexColumnsAsync()).ShouldBeEmpty();
        (await ReadRoundsAsync()).ShouldBe(expected);

        await migrator.MigrateAsync(null, cancellationToken);
        (await IndexColumnsAsync()).ShouldBe(["Season", "Date", "Number"]);
        (await ReadRoundsAsync()).ShouldBe(expected);

        Task<string[]> IndexColumnsAsync() =>
            context
                .Database.SqlQueryRaw<string>(
                    """
                    SELECT COLUMN_NAME AS Value FROM information_schema.statistics
                    WHERE table_schema = DATABASE() AND table_name = 'Round'
                      AND index_name = 'IX_Round_Season_Date_Number'
                    ORDER BY SEQ_IN_INDEX
                    """
                )
                .ToArrayAsync(cancellationToken);

        Task<RoundSnapshot[]> ReadRoundsAsync() =>
            context
                .Rounds.AsNoTracking()
                .OrderBy(round => round.Id)
                .Select(round => new RoundSnapshot(
                    round.Id.Value,
                    round.Season.Value,
                    round.Date,
                    round.Number,
                    round.Venue.Value,
                    round.Status.Value
                ))
                .ToArrayAsync(cancellationToken);
    }

    private sealed record RoundSnapshot(int Id, int Season, DateOnly Date, short Number, string Venue, string Status);

    [Fact]
    public async Task Sport_image_migration_accepts_an_image_column_that_already_exists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await fixture.ExecuteSqlAsync(
            "ALTER TABLE `Sport` ADD COLUMN `Image` longblob NULL; " + "INSERT INTO `Sport` (`Name`) VALUES ('f1')"
        );

        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        (
            await context.Sports.AsNoTracking().Select(sport => sport.Image).SingleAsync(cancellationToken)
        ).ShouldNotBeNull();
    }

    [Fact]
    public async Task Migrator_accepts_auth_columns_that_already_exist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await fixture.ExecuteSqlAsync(
            "ALTER TABLE `User` "
                + "ADD COLUMN `PasswordHashV3` varchar(512) NULL, "
                + "ADD COLUMN `SecurityStamp` varchar(64) NULL, "
                + "ADD COLUMN `ConcurrencyStamp` varchar(64) NULL"
        );

        var factory = fixture.CreateContextFactory();
        await CreateMigrator(factory).MigrateAsync(cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var user = new User { Id = UserId.From("test"), Username = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        (await context.Users.Select(x => x.Id.Value).SingleAsync(cancellationToken)).ShouldBe("test");
    }

    [Fact]
    public async Task Concurrent_migrators_serialize_schema_changes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        var first = CreateMigrator(factory).MigrateAsync(cancellationToken);
        var second = CreateMigrator(factory).MigrateAsync(cancellationToken);

        await Task.WhenAll(first, second);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Count().ShouldBe(8);
    }

    [Fact]
    public async Task DbContext_reads_persisted_data_types()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetSchemaAsync();
        await CreateMigrator(fixture.CreateContextFactory()).MigrateAsync(cancellationToken);
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`, `ISO3`, `NumCode`, `PhoneCode`)
            VALUES ('BE', 'Belgium', 'Belgium', 'BEL', 56, 32);
            INSERT INTO `Sport` (`Name`, `FullName`) VALUES ('Racing', 'Test Racing Series');
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Scheduled', 0);
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Test Circuit', 'BE');
            INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`)
            VALUES ('Mx', 'Casey', 'Racer', 'BE');
            INSERT INTO `Team` (`Name`, `Sport`, `Country`) VALUES ('Test Team', 'Racing', 'BE');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES ('Racing', '2026');
            INSERT INTO `SeasonEntry` (`Season`, `Team`, `Name`) VALUES (1, 1, 'Test Team');
            INSERT INTO `Round` (`Date`, `Number`, `Name`, `Season`, `Venue`, `Status`, `Rating`, `Rain`, `Note`)
            VALUES ('2026-04-05', 1, 'Test Grand Prix', 1, 'Test Circuit', 'Scheduled', 8.0, 2, 'Synthetic fixture');
            INSERT INTO `User` (`Id`, `Username`)
            VALUES ('fixture-user', 'fixture-user');
            """
        );

        var factory = fixture.CreateContextFactory();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var round = await context.Rounds.AsNoTracking().SingleAsync(cancellationToken);
        var country = await context.Countries.AsNoTracking().SingleAsync(cancellationToken);
        var status = await context.Statuses.AsNoTracking().SingleAsync(cancellationToken);
        var user = await context.Users.AsNoTracking().SingleAsync(cancellationToken);

        round.Date.ShouldBe(new DateOnly(2026, 4, 5));
        round.Rating.ShouldBe(8.0m);
        round.Rain.ShouldBe(2m);
        round.Venue.Value.ShouldBe("Test Circuit");
        country.Iso.Value.ShouldBe("BE");
        country.NumCode.ShouldBe((short)56);
        status.Step.ShouldBe((sbyte)0);
        user.Username.ShouldBe("fixture-user");
    }

    private static DatabaseMigrator CreateMigrator(IDbContextFactory<MotorsportsDbContext> factory) =>
        new(factory, NullLogger<DatabaseMigrator>.Instance);
}
