using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Lookups;
using Motorsports.Participants;
using Motorsports.Rounds;
using Motorsports.Seasons;
using Motorsports.Sports;
using Motorsports.Teams;
using Motorsports.Venues;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class RoundCharacteristicsTests(MariaDbFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class Services
    {
        public required IDbContextFactory<MotorsportsDbContext> Factory { get; init; }
        public required LookupCache Cache { get; init; }
        public required SportService Sports { get; init; }
        public required VenueService Venues { get; init; }
        public required TeamService Teams { get; init; }
        public required ParticipantService Participants { get; init; }
        public required SeasonService Seasons { get; init; }
        public required SeasonEntryService Entries { get; init; }
        public required RoundService Rounds { get; init; }
    }

    private async Task<Services> StartAsync()
    {
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await new DatabaseMigrator(factory, NullLogger<DatabaseMigrator>.Instance).MigrateAsync(Ct);
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES ('GB','UNITED KINGDOM','United Kingdom');
            INSERT INTO `Status` (`Name`, `Step`) VALUES
              ('Scheduled', 0),
              ('ReadyToWatch', 1),
              ('Postponed', 10),
              ('Finished', 20);
            """
        );
        var cache = new LookupCache(
            factory,
            TimeProvider.System,
            Options.Create(new LookupCacheOptions { MaxAge = TimeSpan.FromSeconds(30) }),
            NullLogger<LookupCache>.Instance
        );
        return new Services
        {
            Factory = factory,
            Cache = cache,
            Sports = new SportService(factory, cache, new SportFormValidator()),
            Venues = new VenueService(factory, new VenueFormValidator(), cache),
            Teams = new TeamService(factory, new TeamFormValidator(), cache),
            Participants = new ParticipantService(factory, new ParticipantFormValidator(), cache),
            Seasons = new SeasonService(factory, cache, new SeasonFormValidator()),
            Entries = new SeasonEntryService(factory, cache, new SeasonEntryFormValidator()),
            Rounds = new RoundService(factory, new RoundFormValidator()),
        };
    }

    [Fact]
    public async Task Round_create_edit_history_ready_delete_and_conflicts()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        (
            await services.Venues.CreateAsync(new VenueForm { Name = "Spa", Country = "GB" }, Ct)
        ).IsSuccess.ShouldBeTrue();
        var teamId = (
            await services.Teams.CreateAsync(
                new TeamForm
                {
                    Name = "Car",
                    Sport = "F1",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var participantId = (
            await services.Participants.CreateAsync(
                new ParticipantForm
                {
                    Title = "Mr",
                    FirstName = "Max",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var seasonId = (await services.Seasons.CreateAsync(new SeasonForm { Sport = "F1", Label = "2026" }, Ct)).Value;
        (
            await services.Entries.CreateAsync(seasonId, new SeasonEntryForm { TeamId = teamId, Name = "Car 26" }, Ct)
        ).IsSuccess.ShouldBeTrue();

        var created = await services.Rounds.CreateAsync(
            new RoundForm
            {
                SeasonId = seasonId,
                Date = new DateOnly(2026, 3, 1),
                Number = 1,
                Name = "Belgium",
                Venue = "Spa",
                Status = "Scheduled",
            },
            Ct
        );
        created.IsSuccess.ShouldBeTrue();
        var roundId = created.Value;

        var defaults = await services.Rounds.GetCreateFormAsync(seasonId, new DateOnly(2026, 1, 2), Ct);
        defaults!.Date.ShouldBe(new DateOnly(2026, 3, 1));
        defaults.Number.ShouldBe((short)2);

        var duplicate = await services.Rounds.CreateAsync(
            new RoundForm
            {
                SeasonId = seasonId,
                Date = new DateOnly(2026, 4, 1),
                Number = 1,
                Venue = "Spa",
                Status = "Scheduled",
            },
            Ct
        );
        duplicate.Error.Kind.ShouldBe(ErrorKind.Validation);

        var tooEarly = await services.Rounds.CreateAsync(
            new RoundForm
            {
                SeasonId = seasonId,
                Date = new DateOnly(2025, 1, 1),
                Number = 3,
                Venue = "Spa",
                Status = "Scheduled",
            },
            Ct
        );
        tooEarly.Error.Kind.ShouldBe(ErrorKind.Validation);

        (await services.Rounds.MarkReadyToWatchAsync(roundId, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Rounds.GetAsync(roundId, Ct))!.Status.Value.ShouldBe("ReadyToWatch");
        (await services.Rounds.MarkReadyToWatchAsync(roundId, Ct)).IsSuccess.ShouldBeTrue();

        var form = (await services.Rounds.GetFormAsync(roundId, Ct))!;
        form.Status = "Finished";
        form.Rating = 8;
        form.Rain = RainLevel.NoRain;
        form.WinningTeam = teamId;
        form.Winners = [participantId];
        (await services.Rounds.UpdateAsync(roundId, form, Ct)).IsSuccess.ShouldBeTrue();

        var stale = (await services.Rounds.GetFormAsync(roundId, Ct))!;
        var edit = (await services.Rounds.GetFormAsync(roundId, Ct))!;
        edit.Note = "ok";
        (await services.Rounds.UpdateAsync(roundId, edit, Ct)).IsSuccess.ShouldBeTrue();
        stale.Note = "stale";
        (await services.Rounds.UpdateAsync(roundId, stale, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);

        (await services.Rounds.DeleteAsync(roundId, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);

        var later = await services.Rounds.CreateAsync(
            new RoundForm
            {
                SeasonId = seasonId,
                Date = new DateOnly(2026, 5, 1),
                Number = 2,
                Name = "Belgium 2",
                Venue = "Spa",
                Status = "Scheduled",
            },
            Ct
        );
        later.IsSuccess.ShouldBeTrue();
        var laterForm = (await services.Rounds.GetFormAsync(later.Value, Ct))!;
        laterForm.Status = "Finished";
        (await services.Rounds.UpdateAsync(later.Value, laterForm, Ct)).IsSuccess.ShouldBeTrue();

        var historicalRows = string.Join(
            ", ",
            Enumerable
                .Range(0, 55)
                .Select(index =>
                    $"('{new DateOnly(2026, 1, 1).AddDays(index):yyyy-MM-dd}', 3, {seasonId}, 'Spa', 'Finished')"
                )
        );
        await fixture.ExecuteSqlAsync(
            $"INSERT INTO `Round` (`Date`, `Number`, `Season`, `Venue`, `Status`) VALUES {historicalRows}"
        );
        var history = await services.Rounds.GetEventHistoryAsync(later.Value, Ct);
        history.Count.ShouldBe(50);
        history.Select(item => item.Id.Value).ShouldContain(roundId);

        var cleared = (await services.Rounds.GetFormAsync(roundId, Ct))!;
        cleared.WinningTeam = null;
        cleared.Winners = [];
        (await services.Rounds.UpdateAsync(roundId, cleared, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Rounds.DeleteAsync(roundId, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Rounds.GetAsync(roundId, Ct)).ShouldBeNull();

        (await services.Cache.GetVenuesAsync(Ct)).Select(venue => venue.Name.Value).ShouldContain("Spa");
        (await services.Cache.GetStatusesAsync(Ct)).Select(status => status.Name.Value).ShouldContain("Finished");
    }

    [Fact]
    public async Task Round_list_orders_by_date_and_shows_venue_country()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        await services.Venues.CreateAsync(new VenueForm { Name = "Spa", Country = "GB" }, Ct);
        var seasonId = (await services.Seasons.CreateAsync(new SeasonForm { Sport = "F1", Label = "2026" }, Ct)).Value;
        (
            await services.Rounds.CreateAsync(
                new RoundForm
                {
                    SeasonId = seasonId,
                    Date = new DateOnly(2026, 6, 1),
                    Number = 2,
                    Venue = "Spa",
                    Status = "Scheduled",
                },
                Ct
            )
        ).IsSuccess.ShouldBeTrue();
        (
            await services.Rounds.CreateAsync(
                new RoundForm
                {
                    SeasonId = seasonId,
                    Date = new DateOnly(2026, 3, 1),
                    Number = 1,
                    Venue = "Spa",
                    Status = "Scheduled",
                },
                Ct
            )
        ).IsSuccess.ShouldBeTrue();

        var list = await services.Rounds.ListAsync(seasonId, Ct);
        list.Select(round => round.Date).ShouldBe([new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 1)]);
        list[0].VenueCountry.Value.ShouldBe("GB");
    }
}
