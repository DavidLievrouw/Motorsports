using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
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
        ]);
        (await context.Users.CountAsync(cancellationToken)).ShouldBe(0);
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
            .Select(participant => (participant.Id, participant.FirstName))
            .ShouldBe([(1, " Alex "), (2, "Sam")]);
        var roundWinners = await context
            .RoundWinners.AsNoTracking()
            .OrderBy(winner => winner.Round)
            .Select(winner => new { winner.Round, winner.Participant })
            .ToArrayAsync(cancellationToken);
        roundWinners.Select(winner => (winner.Round, winner.Participant)).ShouldBe([(1, 1), (2, 2)]);
        var seasonWinners = await context
            .SeasonWinners.AsNoTracking()
            .OrderBy(winner => winner.Season)
            .Select(winner => new { winner.Season, winner.Participant })
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
            using var stream = typeof(MotorsportsDbContext).Assembly.GetManifestResourceStream(
                $"Motorsports.Infrastructure.Migrations.SportImages.{name}.png"
            )!;
            using var expected = new MemoryStream();
            await stream.CopyToAsync(expected, cancellationToken);
            images[name].ShouldBe(expected.ToArray());
        }
    }

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
        var user = new User
        {
            Id = "test",
            Username = "test",
            PasswordHash = [1],
            Salt = [2],
            Iterations = 1,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        (await context.Users.Select(x => x.Id).SingleAsync(cancellationToken)).ShouldBe("test");
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
        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Count().ShouldBe(4);
    }

    [Fact]
    public async Task DbContext_reads_schema_only_legacy_data_types()
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
            INSERT INTO `User` (`Id`, `Username`, `PasswordHash`, `Salt`, `Iterations`, `Prf`)
            VALUES ('fixture-user', 'fixture-user', X'0102', X'0304', 10000, 'HMACSHA512');
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
        round.Venue.ShouldBe("Test Circuit");
        country.Iso.ShouldBe("BE");
        country.NumCode.ShouldBe((short)56);
        status.Step.ShouldBe((sbyte)0);
        user.PasswordHash.ShouldBe([1, 2]);
        user.Salt.ShouldBe([3, 4]);
    }

    private static DatabaseMigrator CreateMigrator(IDbContextFactory<MotorsportsDbContext> factory) =>
        new(factory, NullLogger<DatabaseMigrator>.Instance);
}
