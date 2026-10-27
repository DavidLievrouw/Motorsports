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
              ('Finished', 20),
              ('Cancelled', 20),
              ('Stopped', 20);
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
        var staleFinish = (await services.Rounds.GetFinishFormAsync(roundId, Ct))!;
        var edit = (await services.Rounds.GetFormAsync(roundId, Ct))!;
        edit.Note = "ok";
        (await services.Rounds.UpdateAsync(roundId, edit, Ct)).IsSuccess.ShouldBeTrue();
        stale.Note = "stale";
        (await services.Rounds.UpdateAsync(roundId, stale, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);
        (await services.Rounds.MarkStoppedAsync(roundId, staleFinish.Values, Ct)).Error.Kind.ShouldBe(
            ErrorKind.Conflict
        );

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
                    $"('{new DateOnly(2026, 1, 1).AddDays(index):yyyy-MM-dd}', {index + 3}, {seasonId}, 'Spa', 'Finished')"
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

    [Theory]
    [InlineData("Finished", "Rating,Rain,WinningTeam,Winners")]
    [InlineData("Postponed", "Note")]
    [InlineData("Stopped", "Rating,Rain")]
    [InlineData("Cancelled", "Note")]
    public async Task Round_outcome_requires_its_own_fields_and_persists_only_results(string outcome, string required)
    {
        var services = await StartFinishAsync();
        var action = OutcomeAction(services.Rounds, outcome);
        var empty = (await services.Rounds.GetFinishFormAsync(1, Ct))!;
        empty.VenueCountry.Value.ShouldBe("GB");
        var missing = await action(1, empty.Values, Ct);
        missing.Error.Kind.ShouldBe(ErrorKind.Validation);
        missing.Error.Fields!.Keys.Order().ShouldBe(required.Split(',').Order());

        foreach (var field in required.Split(','))
        {
            var incomplete = await CompleteFinishFormAsync(services.Rounds);
            switch (field)
            {
                case "Rating":
                    incomplete.Rating = null;
                    break;
                case "Rain":
                    incomplete.Rain = null;
                    break;
                case "WinningTeam":
                    incomplete.WinningTeam = null;
                    break;
                case "Winners":
                    incomplete.Winners = [];
                    break;
                case "Note":
                    incomplete.Note = " \t ";
                    break;
            }

            var invalid = await action(1, incomplete, Ct);
            invalid.Error.Kind.ShouldBe(ErrorKind.Validation);
            invalid.Error.Fields!.Keys.ShouldBe([field]);
        }

        await using var db = await services.Factory.CreateDbContextAsync(Ct);
        var unchanged = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == RoundId.From(1))
            .Select(round => new
            {
                round.Status,
                round.Rating,
                round.Note,
            })
            .SingleAsync(Ct);
        unchanged.Status.Value.ShouldBe("Scheduled");
        unchanged.Rating.ShouldBeNull();
        unchanged.Note.ShouldBeNull();
        (await db.RoundWinners.CountAsync(Ct)).ShouldBe(0);

        var form = await CompleteFinishFormAsync(services.Rounds);
        if (outcome is "Finished" or "Stopped")
        {
            form.Note = null;
        }
        if (outcome != "Finished")
        {
            form.WinningTeam = null;
            form.Winners = [];
        }
        if (outcome is "Postponed" or "Cancelled")
        {
            form.Rating = null;
            form.Rain = null;
        }

        // The outcome command must ignore client changes to the read-only round identity and schedule.
        form.Date = new DateOnly(2027, 1, 1);
        form.OriginalDate = form.Date;
        form.Number = 99;
        form.Name = "Changed";
        form.Venue = "Other";
        form.SeasonId = 999;
        form.Status = "ReadyToWatch";
        (await action(1, form, Ct)).IsSuccess.ShouldBeTrue();

        var saved = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == RoundId.From(1))
            .Select(round => new
            {
                round.Season,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                round.Status,
                round.Rating,
                round.Rain,
                round.Note,
                round.WinningTeam,
            })
            .SingleAsync(Ct);
        saved.Status.Value.ShouldBe(outcome);
        saved.Season.Value.ShouldBe(1);
        saved.Date.ShouldBe(new DateOnly(2025, 3, 1));
        saved.Number.ShouldBe((short)1);
        saved.Name.ShouldBe("Belgium");
        saved.Venue.Value.ShouldBe("Spa");
        saved.Rating.ShouldBe(form.Rating);
        saved.Rain.ShouldBe(form.Rain is { } rain ? (int)rain : null);
        saved.Note.ShouldBe(form.Note?.Trim());
        (saved.WinningTeam?.Value).ShouldBe(form.WinningTeam);
        var winners = await db
            .RoundWinners.AsNoTracking()
            .Where(winner => winner.Round == RoundId.From(1))
            .Select(winner => winner.Participant)
            .ToListAsync(Ct);
        winners.Select(winner => winner.Value).ShouldBe(form.Winners);

        var full = await CompleteFinishFormAsync(services.Rounds);
        (await action(1, full, Ct)).IsSuccess.ShouldBeTrue();
        var loaded = (await services.Rounds.GetFinishFormAsync(1, Ct))!.Values;
        loaded.Rating.ShouldBe(0);
        loaded.Rain.ShouldBe(RainLevel.NoRain);
        loaded.WinningTeam.ShouldBe(1);
        loaded.Winners.ShouldBe([1]);
        loaded.Note.ShouldBe("Weather");

        if (outcome != "Finished")
        {
            loaded.WinningTeam = null;
            loaded.Winners = [];
            if (outcome == "Stopped")
            {
                loaded.Note = null;
            }
            else
            {
                loaded.Rating = null;
                loaded.Rain = null;
            }
            (await action(1, loaded, Ct)).IsSuccess.ShouldBeTrue();
            var cleared = (await services.Rounds.GetFinishFormAsync(1, Ct))!.Values;
            cleared.WinningTeam.ShouldBeNull();
            cleared.Winners.ShouldBeEmpty();
            cleared.Rating.ShouldBe(loaded.Rating);
            cleared.Rain.ShouldBe(loaded.Rain);
            cleared.Note.ShouldBe(loaded.Note);
        }

        var stale = await CompleteFinishFormAsync(services.Rounds);
        var edit = (await services.Rounds.GetFormAsync(1, Ct))!;
        edit.Note = "Concurrent edit";
        (await services.Rounds.UpdateAsync(1, edit, Ct)).IsSuccess.ShouldBeTrue();
        (await action(1, stale, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);
        (await action(999, stale, Ct)).Error.Kind.ShouldBe(ErrorKind.NotFound);
        (await services.Rounds.GetFinishFormAsync(999, Ct)).ShouldBeNull();

        edit = (await services.Rounds.GetFormAsync(1, Ct))!;
        edit.Status = "Scheduled";
        (await services.Rounds.UpdateAsync(1, edit, Ct)).IsSuccess.ShouldBeTrue();
        var target = StatusName.From(outcome);
        await db.Statuses.Where(status => status.Name == target).ExecuteDeleteAsync(Ct);
        var unconfigured = await action(1, await CompleteFinishFormAsync(services.Rounds), Ct);
        unconfigured.Error.Kind.ShouldBe(ErrorKind.Validation);
        unconfigured.Error.Fields!.Keys.ShouldBe(["Status"]);
        var statusAfterFailure = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == RoundId.From(1))
            .Select(round => round.Status)
            .SingleAsync(Ct);
        statusAfterFailure.Value.ShouldBe("Scheduled");
    }

    [Theory]
    [InlineData("Finished")]
    [InlineData("Postponed")]
    [InlineData("Stopped")]
    [InlineData("Cancelled")]
    public async Task Round_outcome_rejects_invalid_submitted_results_without_writing(string outcome)
    {
        var services = await StartFinishAsync();
        var action = OutcomeAction(services.Rounds, outcome);
        (Action<RoundForm> Change, string Field)[] cases =
        [
            (form => form.Rating = -1, "Rating"),
            (form => form.Rating = 11, "Rating"),
            (form => form.Rain = (RainLevel)99, "Rain"),
            (form => form.Note = new string('x', 1025), "Note"),
            (form => form.WinningTeam = 2, "WinningTeam"),
            (form => form.Winners = [999], "Winners"),
            (form => form.Winners = [1, 1], "Winners"),
            (form => form.Winners = [1, 2, 3, 4, 5], "Winners"),
        ];
        foreach (var (change, field) in cases)
        {
            var form = await CompleteFinishFormAsync(services.Rounds);
            change(form);
            var result = await action(1, form, Ct);
            result.Error.Kind.ShouldBe(ErrorKind.Validation);
            result.Error.Fields!.Keys.ShouldContain(field);
            await using var db = await services.Factory.CreateDbContextAsync(Ct);
            var saved = await db
                .Rounds.AsNoTracking()
                .Where(round => round.Id == RoundId.From(1))
                .Select(round => new
                {
                    round.Status,
                    round.Rating,
                    round.Note,
                    round.WinningTeam,
                })
                .SingleAsync(Ct);
            saved.Status.Value.ShouldBe("Scheduled");
            saved.Rating.ShouldBeNull();
            saved.Note.ShouldBeNull();
            saved.WinningTeam.ShouldBeNull();
            (await db.RoundWinners.CountAsync(Ct)).ShouldBe(0);
        }

        var limits = await CompleteFinishFormAsync(services.Rounds);
        limits.Rating = 10;
        limits.Rain = RainLevel.CancelledDueToHeavyRain;
        limits.Note = new string('x', 1024);
        limits.Winners = [1, 2, 3, 4];
        (await action(1, limits, Ct)).IsSuccess.ShouldBeTrue();
        var accepted = (await services.Rounds.GetFinishFormAsync(1, Ct))!.Values;
        accepted.Rating.ShouldBe(10);
        accepted.Rain.ShouldBe(RainLevel.CancelledDueToHeavyRain);
        accepted.Note!.Length.ShouldBe(1024);
        accepted.Winners.ShouldBe([1, 2, 3, 4]);
    }

    private async Task<Services> StartFinishAsync()
    {
        var services = await StartAsync();
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Sport` (`Name`) VALUES ('F1');
            INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Spa', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`) VALUES ('F1', '2025'), ('F1', '2026');
            INSERT INTO `Team` (`Name`, `Sport`, `Country`) VALUES ('Car', 'F1', 'GB'), ('Other', 'F1', 'GB');
            INSERT INTO `SeasonEntry` (`Season`, `Team`, `Name`) VALUES (1, 1, 'Car 25'), (2, 2, 'Other 26');
            INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`) VALUES
              ('', 'Max', 'Driver', 'GB'), ('', 'Alex', 'Driver', 'GB'), ('', 'Sam', 'Driver', 'GB'),
              ('', 'Pat', 'Driver', 'GB'), ('', 'Chris', 'Driver', 'GB');
            INSERT INTO `Round` (`Date`, `Number`, `Name`, `Season`, `Venue`, `Status`)
              VALUES ('2025-03-01', 1, 'Belgium', 1, 'Spa', 'Scheduled');
            """
        );
        return services;
    }

    private static async Task<RoundForm> CompleteFinishFormAsync(RoundService rounds)
    {
        var form = (await rounds.GetFinishFormAsync(1, Ct))!.Values;
        form.Rating = 0;
        form.Rain = RainLevel.NoRain;
        form.WinningTeam = 1;
        form.Winners = [1];
        form.Note = "  Weather  ";
        return form;
    }

    private static Func<int, RoundForm, CancellationToken, Task<Result<int>>> OutcomeAction(
        RoundService rounds,
        string outcome
    ) =>
        outcome switch
        {
            "Finished" => rounds.MarkFinishedAsync,
            "Postponed" => rounds.MarkPostponedAsync,
            "Stopped" => rounds.MarkStoppedAsync,
            "Cancelled" => rounds.MarkCancelledAsync,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

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
