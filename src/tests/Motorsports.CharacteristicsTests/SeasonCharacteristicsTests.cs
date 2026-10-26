using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Lookups;
using Motorsports.Participants;
using Motorsports.Seasons;
using Motorsports.Sports;
using Motorsports.Teams;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class SeasonCharacteristicsTests(MariaDbFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class Services
    {
        public required IDbContextFactory<MotorsportsDbContext> Factory { get; init; }
        public required LookupCache Cache { get; init; }
        public required SportService Sports { get; init; }
        public required TeamService Teams { get; init; }
        public required ParticipantService Participants { get; init; }
        public required SeasonService Seasons { get; init; }
        public required SeasonEntryService Entries { get; init; }
    }

    private async Task<Services> StartAsync(TimeSpan? maxAge = null)
    {
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await new DatabaseMigrator(factory, NullLogger<DatabaseMigrator>.Instance).MigrateAsync(Ct);
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Country` (`ISO`, `Name`, `NiceName`) VALUES ('GB','UNITED KINGDOM','United Kingdom');
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Scheduled', 0);
            """
        );
        var cache = new LookupCache(
            factory,
            TimeProvider.System,
            Options.Create(new LookupCacheOptions { MaxAge = maxAge ?? TimeSpan.FromSeconds(30) }),
            NullLogger<LookupCache>.Instance
        );
        return new Services
        {
            Factory = factory,
            Cache = cache,
            Sports = new SportService(factory, cache, new SportFormValidator()),
            Teams = new TeamService(factory, new TeamFormValidator(), cache),
            Participants = new ParticipantService(factory, new ParticipantFormValidator(), cache),
            Seasons = new SeasonService(factory, cache, new SeasonFormValidator()),
            Entries = new SeasonEntryService(factory, cache, new SeasonEntryFormValidator()),
        };
    }

    [Fact]
    public async Task Season_winners_are_validated_replaced_and_protected_by_concurrency()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        await services.Sports.CreateAsync(new SportForm { Name = "Rally" }, Ct);
        (await services.Cache.GetParticipantsAsync(Ct)).ShouldBeEmpty();
        var carId = (
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
        var otherSportTeamId = (
            await services.Teams.CreateAsync(
                new TeamForm
                {
                    Name = "Rally car",
                    Sport = "Rally",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var participantId = (
            await services.Participants.CreateAsync(
                new ParticipantForm
                {
                    FirstName = "Alex",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        (await services.Cache.GetParticipantsAsync(Ct))
            .Select(participant => participant.FirstName + " " + participant.LastName)
            .ShouldBe(["Alex Driver"]);
        var secondParticipantId = (
            await services.Participants.CreateAsync(
                new ParticipantForm
                {
                    FirstName = "Sam",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var seasonId = (
            await services.Seasons.CreateAsync(new SeasonForm { Sport = "f1", Label = "  2026  " }, Ct)
        ).Value;

        var wrongSport = await services.Entries.CreateAsync(
            seasonId,
            new SeasonEntryForm { TeamId = otherSportTeamId, Name = "Rally" },
            Ct
        );
        wrongSport.IsSuccess.ShouldBeFalse();
        wrongSport.Error.Kind.ShouldBe(ErrorKind.Validation);

        var createdEntry = await services.Entries.CreateAsync(
            seasonId,
            new SeasonEntryForm { TeamId = carId, Name = "Scuderia" },
            Ct
        );
        createdEntry.IsSuccess.ShouldBeTrue();
        (await services.Cache.GetTeamsAsync(Ct))
            .Single(team => team.Id == TeamId.From(carId))
            .History.Single()
            .Name.ShouldBe("Scuderia");
        (await services.Cache.GetSeasonEntriesAsync(Ct)).Single().Name.ShouldBe("Scuderia");

        var form = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        form.WinningTeam = otherSportTeamId;
        form.Winners = [participantId];
        var invalidTeam = await services.Seasons.UpdateAsync(seasonId, form, Ct);
        invalidTeam.IsSuccess.ShouldBeFalse();
        invalidTeam.Error.Fields!.ContainsKey(nameof(SeasonForm.WinningTeam)).ShouldBeTrue();

        form.WinningTeam = carId;
        form.Winners = [int.MaxValue];
        var invalidWinner = await services.Seasons.UpdateAsync(seasonId, form, Ct);
        invalidWinner.IsSuccess.ShouldBeFalse();
        invalidWinner.Error.Fields!.ContainsKey(nameof(SeasonForm.Winners)).ShouldBeTrue();

        form.Winners = [participantId, participantId];
        var duplicateWinner = await services.Seasons.UpdateAsync(seasonId, form, Ct);
        duplicateWinner.IsSuccess.ShouldBeFalse();
        duplicateWinner.Error.Fields!.ContainsKey(nameof(SeasonForm.Winners)).ShouldBeTrue();

        form.Winners = [participantId, secondParticipantId];
        (await services.Seasons.UpdateAsync(seasonId, form, Ct)).IsSuccess.ShouldBeTrue();
        var current = (await services.Seasons.GetAsync(seasonId, Ct))!;
        current.Label.ShouldBe("2026");
        current.WinningTeamName.ShouldBe("\U0001F1EC\U0001F1E7 Scuderia");
        current.WinningParticipants.ShouldBe(["\U0001F1EC\U0001F1E7 Alex Driver", "\U0001F1EC\U0001F1E7 Sam Driver"]);

        var stale = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        var fresh = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        fresh.Label = "2027";
        (await services.Seasons.UpdateAsync(seasonId, fresh, Ct)).IsSuccess.ShouldBeTrue();
        stale.Label = "stale";
        var conflict = await services.Seasons.UpdateAsync(seasonId, stale, Ct);
        conflict.Error.Kind.ShouldBe(ErrorKind.Conflict);

        var immutableSport = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        immutableSport.Sport = "Rally";
        var rejectedSport = await services.Seasons.UpdateAsync(seasonId, immutableSport, Ct);
        rejectedSport.Error.Kind.ShouldBe(ErrorKind.Validation);

        (await services.Entries.DeleteAsync(seasonId, carId, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Season_delete_removes_entries_and_winners_but_is_blocked_by_rounds()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        var teamId = (
            await services.Teams.CreateAsync(
                new TeamForm
                {
                    Name = "Team",
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
                    FirstName = "Alex",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var seasonId = (await services.Seasons.CreateAsync(new SeasonForm { Sport = "F1", Label = "2026" }, Ct)).Value;
        await services.Entries.CreateAsync(seasonId, new SeasonEntryForm { TeamId = teamId, Name = "Team 2026" }, Ct);
        var form = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        form.WinningTeam = teamId;
        form.Winners = [participantId];
        (await services.Seasons.UpdateAsync(seasonId, form, Ct)).IsSuccess.ShouldBeTrue();

        var deletion = (await services.Seasons.GetDeleteInfoAsync(seasonId, Ct))!;
        (deletion.EntryCount, deletion.WinnerCount, deletion.RoundCount).ShouldBe((1, 1, 0));
        (await services.Seasons.DeleteAsync(seasonId, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Seasons.GetAsync(seasonId, Ct)).ShouldBeNull();
        (await services.Cache.GetSeasonEntriesAsync(Ct)).ShouldBeEmpty();
        (await services.Cache.GetTeamsAsync(Ct)).Single().History.ShouldBeEmpty();

        var seasonWithRound = (
            await services.Seasons.CreateAsync(new SeasonForm { Sport = "F1", Label = "2027" }, Ct)
        ).Value;
        await VenuesCreateAsyncForTest(fixture);
        await fixture.ExecuteSqlAsync(
            $"INSERT INTO `Round` (`Date`, `Number`, `Season`, `Venue`, `Status`) VALUES ('2027-01-01', 1, {seasonWithRound}, 'Test venue', 'Scheduled')"
        );
        var computedDates = (await services.Seasons.GetAsync(seasonWithRound, Ct))!;
        computedDates.StartDate.ShouldBe(new DateOnly(2027, 1, 1));
        computedDates.EndDate.ShouldBe(new DateOnly(2027, 1, 1));
        (await services.Seasons.ListAsync(cancellationToken: Ct)).Single().StartDate.ShouldBe(new DateOnly(2027, 1, 1));
        (await services.Seasons.DeleteAsync(seasonWithRound, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);
        (await services.Seasons.GetAsync(seasonWithRound, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Season_entry_update_and_delete_validate_name_and_allow_only_non_winners_to_delete()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        var teamId = (
            await services.Teams.CreateAsync(
                new TeamForm
                {
                    Name = "Team",
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
                    FirstName = "Alex",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        var seasonId = (await services.Seasons.CreateAsync(new SeasonForm { Sport = "F1", Label = "2026" }, Ct)).Value;
        (
            await services.Entries.CreateAsync(seasonId, new SeasonEntryForm { TeamId = teamId, Name = "Original" }, Ct)
        ).IsSuccess.ShouldBeTrue();

        var stale = (await services.Entries.GetFormAsync(seasonId, teamId, Ct))!;
        var edit = (await services.Entries.GetFormAsync(seasonId, teamId, Ct))!;
        edit.Name = "Edited";
        (await services.Entries.UpdateAsync(seasonId, teamId, edit, Ct)).IsSuccess.ShouldBeTrue();
        stale.Name = "Stale";
        (await services.Entries.UpdateAsync(seasonId, teamId, stale, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);

        var invalid = (await services.Entries.GetFormAsync(seasonId, teamId, Ct))!;
        invalid.Name = new string('x', SeasonEntryFormValidator.MaxNameLength + 1);
        (await services.Entries.UpdateAsync(seasonId, teamId, invalid, Ct)).Error.Kind.ShouldBe(ErrorKind.Validation);

        var season = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        season.WinningTeam = teamId;
        season.Winners = [participantId];
        (await services.Seasons.UpdateAsync(seasonId, season, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Entries.DeleteAsync(seasonId, teamId, Ct)).Error.Kind.ShouldBe(ErrorKind.Conflict);

        season = (await services.Seasons.GetFormAsync(seasonId, Ct))!;
        season.WinningTeam = null;
        season.Winners = [];
        (await services.Seasons.UpdateAsync(seasonId, season, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Entries.DeleteAsync(seasonId, teamId, Ct)).IsSuccess.ShouldBeTrue();
        (await services.Entries.GetAsync(seasonId, teamId, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Season_form_validates_trimmed_label_and_maximum_length()
    {
        var services = await StartAsync();
        await services.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);

        var tooLong = await services.Seasons.CreateAsync(
            new SeasonForm { Sport = "F1", Label = new string('x', SeasonFormValidator.MaxLabelLength + 1) },
            Ct
        );
        tooLong.Error.Fields!.ContainsKey(nameof(SeasonForm.Label)).ShouldBeTrue();

        var created = await services.Seasons.CreateAsync(
            new SeasonForm { Sport = "F1", Label = $" {new string('x', SeasonFormValidator.MaxLabelLength)} " },
            Ct
        );
        created.IsSuccess.ShouldBeTrue();
        (await services.Seasons.GetAsync(created.Value, Ct))!.Label!.Length.ShouldBe(
            SeasonFormValidator.MaxLabelLength
        );
    }

    [Fact]
    public async Task Participant_lookup_refreshes_external_legacy_writes_after_max_age()
    {
        var services = await StartAsync(TimeSpan.FromMilliseconds(50));
        (await services.Cache.GetParticipantsAsync(Ct)).ShouldBeEmpty();

        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`) VALUES ('', 'Legacy', 'Driver', 'GB')"
        );
        await Task.Delay(100, Ct);
        await services.Cache.GetParticipantsAsync(Ct);
        await Task.Delay(250, Ct);

        (await services.Cache.GetParticipantsAsync(Ct))
            .Select(participant => participant.FirstName + " " + participant.LastName)
            .ShouldBe(["Legacy Driver"]);
    }

    private static async Task VenuesCreateAsyncForTest(MariaDbFixture fixture)
    {
        await fixture.ExecuteSqlAsync("INSERT INTO `Venue` (`Name`, `Country`) VALUES ('Test venue', 'GB')");
    }
}
