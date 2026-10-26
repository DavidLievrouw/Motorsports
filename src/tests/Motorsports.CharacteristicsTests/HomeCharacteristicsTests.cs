using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Home;
using Motorsports.Infrastructure;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class HomeCharacteristicsTests(MariaDbFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Home_overview_selects_due_rounds_acquisitions_and_ten_year_event_history_in_constant_queries()
    {
        await fixture.ResetSchemaAsync();
        await new DatabaseMigrator(fixture.CreateContextFactory(), NullLogger<DatabaseMigrator>.Instance).MigrateAsync(
            Ct
        );
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES
              ('GB', 'UNITED KINGDOM', 'United Kingdom'),
              ('IT', 'ITALY', 'Italy'),
              ('BE', 'BELGIUM', 'Belgium');
            INSERT INTO `Sport` (`Name`, `FullName`) VALUES
              ('F1', 'Formula One'),
              ('MotoGP', 'Motorcycle Grand Prix');
            INSERT INTO `Status` (`Name`, `Step`) VALUES
              ('Scheduled', 0),
              ('ReadyToWatch', 1),
              ('Postponed', 10),
              ('Finished', 20);
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Spa', 'BE'), ('Monza', 'IT');
            """
        );
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Team` (`Name`, `Sport`, `Country`) VALUES ('Ducati', 'MotoGP', 'IT'), ('Ferrari', 'F1', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES
              ('F1', '2025'),
              ('F1', '2026'),
              ('F1', 'Empty'),
              ('MotoGP', '2025'),
              ('MotoGP', '2026');
            INSERT INTO `SeasonEntry` (`Season`, `Team`, `Name`) VALUES
              (4, 1, 'Ducati GP25'),
              (5, 1, 'Ducati GP26');
            INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`)
              VALUES ('', 'Andrea', 'Rider', 'IT');
            INSERT INTO `Round` (`Date`, `Number`, `Name`, `Season`, `Venue`, `Status`, `WinningTeam`) VALUES
              ('2025-10-20', 1, 'F1 history', 1, 'Monza', 'Finished', NULL),
              ('2026-10-22', 0, 'Older non-championship event', 4, 'Spa', 'Finished', 1),
              ('2026-10-21', 1, 'Championship event', 4, 'Spa', 'Finished', 1),
              ('2026-10-22', 0, 'Postponed event', 4, 'Spa', 'Postponed', NULL),
              ('2026-10-24', 0, 'Future history event', 4, 'Spa', 'Finished', NULL),
              ('2026-10-23', 0, 'Very next event', 5, 'Spa', 'Scheduled', NULL),
              ('2026-10-24', 1, 'F1 due event', 2, 'Spa', 'Scheduled', NULL);
            INSERT INTO `Round` (`Date`, `Number`, `Name`, `Season`, `Venue`, `Status`, `Rating`, `Rain`, `WinningTeam`) VALUES
              ('2025-10-24', 1, 'F1 Spa history', 1, 'Spa', 'Finished', 8, 2, 2),
              ('2016-10-23', 1, 'Outside ten years', 1, 'Spa', 'Finished', 5, 1, NULL),
              ('2016-10-24', 1, 'At ten-year boundary', 1, 'Spa', 'Finished', 7, 3, NULL);
            INSERT INTO `RoundWinner` (`Round`, `Participant`) VALUES (2, 1), (8, 1);
            """
        );

        var queryCounter = new QueryCounter();
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, fixture.ConnectionString);
        options.AddInterceptors(queryCounter);
        var factory = new PooledDbContextFactory<MotorsportsDbContext>(options.Options);

        var overview = await new HomeService(factory).GetHomeOverviewAsync(new DateOnly(2026, 10, 25), Ct);

        overview.VeryNextUp.ShouldNotBeNull();
        overview.VeryNextUp.Id.Value.ShouldBe(6);
        overview.VeryNextUp.Sport.Value.ShouldBe("MotoGP");
        overview.NextUp.Select(round => round.Sport.Value).ShouldBe(["F1"]);
        overview.RoundsToAcquire.Select(round => round.Id.Value).ShouldBe([6, 7]);
        var f1History = overview.NextUp.Single().EventHistory;
        f1History.Select(item => item.Id.Value).ShouldBe([8, 10]);
        var historicalWin = f1History.Single(item => item.Id.Value == 8);
        historicalWin.Rating.ShouldBe(8m);
        historicalWin.Rain.ShouldBe(2m);
        historicalWin.WinningTeamName!.ShouldContain("Ferrari");
        historicalWin.Winners.Single().ShouldContain("Andrea Rider");
        queryCounter.Count.ShouldBe(4);
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        private int count;

        public int Count => Volatile.Read(ref count);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref count);
            return ValueTask.FromResult(result);
        }
    }
}
