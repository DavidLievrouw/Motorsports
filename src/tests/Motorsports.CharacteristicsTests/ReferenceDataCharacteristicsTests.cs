using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Motorsports.CharacteristicsTests.Fixtures;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Lookups;
using Motorsports.Participants;
using Motorsports.Sports;
using Motorsports.Teams;
using Motorsports.Venues;
using Shouldly;

namespace Motorsports.CharacteristicsTests;

[Collection(MariaDbCollection.Name)]
public sealed class ReferenceDataCharacteristicsTests(MariaDbFixture fixture)
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
    }

    private async Task<Services> StartAsync(TimeSpan? maxAge = null)
    {
        await fixture.ResetSchemaAsync();
        var factory = fixture.CreateContextFactory();
        await new DatabaseMigrator(factory, NullLogger<DatabaseMigrator>.Instance).MigrateAsync(Ct);
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Country` (ISO, Name, NiceName) VALUES ('GB','UNITED KINGDOM','United Kingdom'),('FR','FRANCE','France'),('DE','GERMANY','Germany');"
                + "INSERT INTO `Status` (Name, Step) VALUES ('Scheduled', 0)"
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
            Venues = new VenueService(factory, new VenueFormValidator(), cache),
            Teams = new TeamService(factory, new TeamFormValidator(), cache),
            Participants = new ParticipantService(factory, new ParticipantFormValidator(), cache),
        };
    }

    private static void ShouldFailWith(Error error, ErrorKind kind, string? field = null)
    {
        error.Kind.ShouldBe(kind);
        if (field is not null)
        {
            error.Fields.ShouldNotBeNull();
            error.Fields!.ContainsKey(field).ShouldBeTrue();
        }
    }

    private async Task<long> CountAsync(string sql)
    {
        await using var db = await fixture.CreateContextFactory().CreateDbContextAsync(Ct);
#pragma warning disable EF1003 // Callers supply test-authored SQL fragments, not user input.
        return await db
            .Database.SqlQueryRaw<long>("SELECT CAST(COUNT(*) AS SIGNED) AS `Value` FROM (" + sql + ") x")
            .SingleAsync(Ct);
#pragma warning restore EF1003
    }

    // ---------- Sports ----------

    [Fact]
    public async Task Sport_create_trims_and_rejects_duplicates_case_and_accent_insensitively()
    {
        var s = await StartAsync();
        var created = await s.Sports.CreateAsync(new SportForm { Name = "  Formula 1  ", FullName = " F1 World " }, Ct);
        created.IsSuccess.ShouldBeTrue();
        created.Value.ShouldBe("Formula 1");
        (await s.Sports.GetAsync("Formula 1", Ct))!.FullName.ShouldBe("F1 World");

        var duplicate = await s.Sports.CreateAsync(new SportForm { Name = "FORMULA 1" }, Ct);
        duplicate.IsSuccess.ShouldBeFalse();
        ShouldFailWith(duplicate.Error, ErrorKind.Validation, nameof(SportForm.Name));
        (await s.Sports.CreateAsync(new SportForm { Name = "Café" }, Ct)).IsSuccess.ShouldBeTrue();
        (await s.Sports.CreateAsync(new SportForm { Name = "Cafe" }, Ct)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Sport_create_validates_lengths_and_required_name()
    {
        var s = await StartAsync();
        ShouldFailWith(
            (await s.Sports.CreateAsync(new SportForm { Name = new string('a', 51) }, Ct)).Error,
            ErrorKind.Validation,
            nameof(SportForm.Name)
        );
        ShouldFailWith(
            (await s.Sports.CreateAsync(new SportForm { Name = "x", FullName = new string('a', 101) }, Ct)).Error,
            ErrorKind.Validation,
            nameof(SportForm.FullName)
        );
        ShouldFailWith(
            (await s.Sports.CreateAsync(new SportForm { Name = "   " }, Ct)).Error,
            ErrorKind.Validation,
            nameof(SportForm.Name)
        );
        (
            await s.Sports.CreateAsync(
                new SportForm { Name = new string('a', 50), FullName = new string('b', 100) },
                Ct
            )
        ).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Sport_images_are_validated_persisted_and_included_in_concurrency_checks()
    {
        var s = await StartAsync();
        var image = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1 };
        var otherImage = new byte[] { 255, 216, 255, 1, 2 };
        var created = await s.Sports.CreateAsync(new SportForm { Name = "F1", Image = image }, Ct);
        created.IsSuccess.ShouldBeTrue();
        (await s.Sports.GetAsync("F1", Ct))!.HasImage.ShouldBeTrue();

        var stale = (await s.Sports.GetFormAsync("F1", Ct))!;
        var fresh = (await s.Sports.GetFormAsync("F1", Ct))!;
        fresh.Image = otherImage;
        (await s.Sports.UpdateAsync("F1", fresh, Ct)).IsSuccess.ShouldBeTrue();
        stale.FullName = "Stale edit";
        ShouldFailWith((await s.Sports.UpdateAsync("F1", stale, Ct)).Error, ErrorKind.Conflict);
        (await s.Sports.GetFormAsync("F1", Ct))!.Image.ShouldBe(otherImage);

        var clear = (await s.Sports.GetFormAsync("F1", Ct))!;
        clear.Image = null;
        (await s.Sports.UpdateAsync("F1", clear, Ct)).IsSuccess.ShouldBeTrue();
        (await s.Sports.GetAsync("F1", Ct))!.HasImage.ShouldBeFalse();

        var invalid = await s.Sports.CreateAsync(new SportForm { Name = "Invalid", Image = [1, 2, 3] }, Ct);
        ShouldFailWith(invalid.Error, ErrorKind.Validation, nameof(SportForm.Image));
    }

    [Fact]
    public async Task Sport_rename_without_dependents_replaces_the_key()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "Karting" }, Ct);
        var form = (await s.Sports.GetFormAsync("Karting", Ct))!;
        form.Name = "Go Karting";

        var result = await s.Sports.UpdateAsync("Karting", form, Ct);

        result.IsSuccess.ShouldBeTrue();
        (await s.Sports.GetAsync("Karting", Ct)).ShouldBeNull();
        (await s.Sports.GetAsync("Go Karting", Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Sport_rename_repoints_teams_and_seasons_including_case_only_rename()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "Rally" }, Ct);
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Team` (Name, Sport, Country) VALUES ('Alpha','Rally','GB');"
                + "INSERT INTO `Season` (Sport, Label) VALUES ('Rally','2020')"
        );

        var form = (await s.Sports.GetFormAsync("Rally", Ct))!;
        form.Name = "RALLY";
        (await s.Sports.UpdateAsync("Rally", form, Ct)).IsSuccess.ShouldBeTrue();

        await using (var db = await s.Factory.CreateDbContextAsync(Ct))
        {
            (await db.Sports.AsNoTracking().Select(x => x.Name.Value).ToListAsync(Ct)).ShouldBe(["RALLY"]);
            (await db.Teams.AsNoTracking().Select(x => x.Sport.Value).SingleAsync(Ct)).ShouldBe("RALLY");
            (await db.Seasons.AsNoTracking().Select(x => x.Sport.Value).SingleAsync(Ct)).ShouldBe("RALLY");
        }

        var form2 = (await s.Sports.GetFormAsync("RALLY", Ct))!;
        form2.Name = "Rallye";
        (await s.Sports.UpdateAsync("RALLY", form2, Ct)).IsSuccess.ShouldBeTrue();
        (await CountAsync("SELECT 1 FROM `Team` WHERE Sport = 'Rallye'")).ShouldBe(1);
        (await CountAsync("SELECT 1 FROM `Sport`")).ShouldBe(1);
    }

    [Fact]
    public async Task Sport_rename_to_existing_name_is_a_validation_error_and_changes_nothing()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "A" }, Ct);
        await s.Sports.CreateAsync(new SportForm { Name = "B" }, Ct);
        var form = (await s.Sports.GetFormAsync("A", Ct))!;
        form.Name = "b";

        var result = await s.Sports.UpdateAsync("A", form, Ct);

        ShouldFailWith(result.Error, ErrorKind.Validation, nameof(SportForm.Name));
        (await CountAsync("SELECT 1 FROM `Sport`")).ShouldBe(2);
    }

    [Fact]
    public async Task Sport_update_detects_stale_form_and_missing_rows()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "Golf", FullName = "Old" }, Ct);
        var first = (await s.Sports.GetFormAsync("Golf", Ct))!;
        var second = (await s.Sports.GetFormAsync("Golf", Ct))!;
        second.FullName = "Newer";
        (await s.Sports.UpdateAsync("Golf", second, Ct)).IsSuccess.ShouldBeTrue();

        first.FullName = "Mine";
        var stale = await s.Sports.UpdateAsync("Golf", first, Ct);

        ShouldFailWith(stale.Error, ErrorKind.Conflict);
        (await s.Sports.GetAsync("Golf", Ct))!.FullName.ShouldBe("Newer");
        ShouldFailWith((await s.Sports.UpdateAsync("Nope", first, Ct)).Error, ErrorKind.NotFound);
        (await s.Sports.GetAsync("Nope", Ct)).ShouldBeNull();
        (await s.Sports.GetFormAsync("Nope", Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Sport_changes_are_visible_through_the_lookup_cache_immediately()
    {
        var s = await StartAsync();
        (await s.Cache.GetSportsAsync(Ct)).ShouldBeEmpty();

        await s.Sports.CreateAsync(new SportForm { Name = "Tennis" }, Ct);

        (await s.Cache.GetSportsAsync(Ct)).Select(x => x.Name.Value).ShouldBe(["Tennis"]);
    }

    [Fact]
    public async Task Country_lookup_is_sorted_by_nice_name_and_external_changes_appear_after_max_age()
    {
        var s = await StartAsync(TimeSpan.FromMilliseconds(50));
        (await s.Cache.GetCountriesAsync(Ct)).Select(c => c.Name).ShouldBe(["France", "Germany", "United Kingdom"]);

        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Country` (ISO, Name, NiceName) VALUES ('AU','AUSTRALIA','Australia')"
        );
        await Task.Delay(200, Ct);
        await s.Cache.GetCountriesAsync(Ct); // stale entry is served while a refresh runs
        await Task.Delay(300, Ct);

        (await s.Cache.GetCountriesAsync(Ct)).Select(c => c.Iso.Value).ShouldContain("AU");
    }

    // ---------- Venues ----------

    [Fact]
    public async Task Venue_create_validates_country_length_and_duplicates()
    {
        var s = await StartAsync();
        var ok = await s.Venues.CreateAsync(new VenueForm { Name = " Silverstone ", Country = "gb" }, Ct);
        ok.IsSuccess.ShouldBeTrue();
        (await s.Venues.GetAsync("Silverstone", Ct))!.Country.Value.ShouldBe("GB");
        (await s.Venues.GetAsync("Silverstone", Ct))!.CountryName.ShouldBe("United Kingdom");

        ShouldFailWith(
            (await s.Venues.CreateAsync(new VenueForm { Name = "SILVERSTONE", Country = "FR" }, Ct)).Error,
            ErrorKind.Validation,
            nameof(VenueForm.Name)
        );
        ShouldFailWith(
            (await s.Venues.CreateAsync(new VenueForm { Name = "X", Country = "ZZ" }, Ct)).Error,
            ErrorKind.Validation,
            nameof(VenueForm.Country)
        );
        ShouldFailWith(
            (await s.Venues.CreateAsync(new VenueForm { Name = new string('a', 101), Country = "GB" }, Ct)).Error,
            ErrorKind.Validation,
            nameof(VenueForm.Name)
        );
    }

    [Fact]
    public async Task Venue_rename_repoints_rounds_including_case_only_rename()
    {
        var s = await StartAsync();
        await s.Venues.CreateAsync(new VenueForm { Name = "Spa", Country = "FR" }, Ct);
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Sport` (Name) VALUES ('F1');INSERT INTO `Season` (Sport, Label) VALUES ('F1','2020');"
                + "INSERT INTO `Round` (Date, Number, Season, Venue) VALUES ('2020-01-01',1,1,'Spa')"
        );

        var form = (await s.Venues.GetFormAsync("Spa", Ct))!;
        form.Name = "SPA";
        (await s.Venues.UpdateAsync("Spa", form, Ct)).IsSuccess.ShouldBeTrue();
        (await CountAsync("SELECT 1 FROM `Round` WHERE Venue = 'SPA' AND BINARY Venue = 'SPA'")).ShouldBe(1);

        var form2 = (await s.Venues.GetFormAsync("SPA", Ct))!;
        form2.Name = "Spa-Francorchamps";
        form2.Country = "DE";
        (await s.Venues.UpdateAsync("SPA", form2, Ct)).IsSuccess.ShouldBeTrue();
        (await CountAsync("SELECT 1 FROM `Round` WHERE Venue = 'Spa-Francorchamps'")).ShouldBe(1);
        (await s.Venues.GetAsync("Spa-Francorchamps", Ct))!.RoundCount.ShouldBe(1);
        (await CountAsync("SELECT 1 FROM `Venue`")).ShouldBe(1);
    }

    [Fact]
    public async Task Venue_update_reports_duplicates_stale_forms_and_not_found()
    {
        var s = await StartAsync();
        await s.Venues.CreateAsync(new VenueForm { Name = "A", Country = "GB" }, Ct);
        await s.Venues.CreateAsync(new VenueForm { Name = "B", Country = "GB" }, Ct);
        var a = (await s.Venues.GetFormAsync("A", Ct))!;
        var stale = (await s.Venues.GetFormAsync("A", Ct))!;

        a.Country = "FR";
        (await s.Venues.UpdateAsync("A", a, Ct)).IsSuccess.ShouldBeTrue();
        stale.Name = "B";
        ShouldFailWith((await s.Venues.UpdateAsync("A", stale, Ct)).Error, ErrorKind.Conflict);

        var fresh = (await s.Venues.GetFormAsync("A", Ct))!;
        fresh.Name = "b";
        ShouldFailWith(
            (await s.Venues.UpdateAsync("A", fresh, Ct)).Error,
            ErrorKind.Validation,
            nameof(VenueForm.Name)
        );
        ShouldFailWith((await s.Venues.UpdateAsync("Missing", fresh, Ct)).Error, ErrorKind.NotFound);
    }

    [Fact]
    public async Task Sport_and_venue_details_include_latest_results_next_rounds_and_utf8_names()
    {
        var s = await StartAsync();
        var sportImage = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 8 };
        await s.Sports.CreateAsync(new SportForm { Name = "Racing", Image = sportImage }, Ct);
        const string venueName = "Autódromo Hermanos Rodríguez, Mexico City";
        (await s.Venues.CreateAsync(new VenueForm { Name = venueName, Country = "GB" }, Ct)).IsSuccess.ShouldBeTrue();
        await fixture.ExecuteSqlAsync(
            """
            INSERT INTO `Status` (`Name`, `Step`) VALUES ('Finished', 20);
            INSERT INTO `Team` (`Name`, `Sport`, `Country`) VALUES ('Team Racing', 'Racing', 'GB');
            INSERT INTO `Participant` (`Title`, `FirstName`, `LastName`, `Country`)
            VALUES ('', 'Casey', 'Racer', 'GB');
            INSERT INTO `Season` (`Sport`, `Label`, `WinningTeam`)
            VALUES ('Racing', '2025', 1), ('Racing', '2026', 1);
            INSERT INTO `SeasonEntry` (`Season`, `Team`, `Name`)
            VALUES (1, 1, 'Team 2025'), (2, 1, 'Team 2026');
            INSERT INTO `SeasonWinner` (`Season`, `Participant`) VALUES (1, 1), (2, 1);
            INSERT INTO `Round` (`Date`, `Number`, `Name`, `Season`, `Venue`, `Status`, `Rating`, `Rain`, `WinningTeam`)
            VALUES
                ('2025-04-01', 1, 'Past round', 1, 'Autódromo Hermanos Rodríguez, Mexico City', 'Finished', 8, 2, 1),
                ('2026-05-02', 1, 'Next round', 2, 'Autódromo Hermanos Rodríguez, Mexico City', 'Scheduled', NULL, NULL, NULL);
            INSERT INTO `RoundWinner` (`Round`, `Participant`) VALUES (1, 1);
            """
        );

        var sport = (await s.Sports.GetAsync("Racing", Ct))!;
        sport.LatestSeasonLabel.ShouldBe("2025");
        sport.WinningParticipants.ShouldHaveSingleItem().ShouldEndWith("Casey Racer");
        (await s.Participants.GetAsync(1, Ct))
            ?.Sports.Select(sportName => sportName.Value)
            .ShouldBeSubsetOf(["Racing"]);
        sport.WinningTeamName!.ShouldEndWith("Team 2025");
        sport.NextRoundDate.ShouldBe(new DateOnly(2026, 5, 2));
        sport.HasImage.ShouldBeTrue();

        var venue = (await s.Venues.GetAsync(venueName, Ct))!;
        venue.Name.Value.ShouldBe(venueName);
        venue.RoundCount.ShouldBe(2);
        venue.Rounds[0].Name.ShouldBe("Next round");
        venue.Rounds[0].Label.ShouldBe("Round 1 - Next round");
        venue.Rounds[0].SportHasImage.ShouldBeTrue();
        venue.Rounds[0].WinningTeamName.ShouldBeNull();
        venue.Rounds[0].WinningParticipants.ShouldBeEmpty();
        venue.Rounds[0].Rating.ShouldBeNull();
        venue.Rounds[0].Rain.ShouldBeNull();
        venue.Rounds[1].WinningParticipants.ShouldHaveSingleItem().ShouldEndWith("Casey Racer");
        venue.Rounds[1].WinningParticipants[0].ShouldNotBe("Casey Racer");
        venue.Rounds[1].WinningTeamName!.ShouldEndWith("Team 2025");
        venue.Rounds[1].WinningTeamName.ShouldNotBe("Team 2025");
        venue.Rounds[1].Status.Value.ShouldBe("Finished");
        venue.Rounds[1].Rating.ShouldBe(8m);
        venue.Rounds[1].Label.ShouldBe("Round 1 - Past round");
        (await s.Teams.GetAsync(1, Ct))!.SeasonWins.Select(win => win.SeasonId.Value).ShouldBe([2, 1]);
        venue.Rounds[1].Rain.ShouldBe("Rain without impact");
    }

    // ---------- Teams ----------

    private static TeamForm NewTeam(string name = "Ferrari", string sport = "F1", string country = "GB") =>
        new()
        {
            Name = name,
            Sport = sport,
            Country = country,
        };

    [Fact]
    public async Task Team_create_validates_foreign_keys_lengths_and_uniqueness_per_sport()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        await s.Sports.CreateAsync(new SportForm { Name = "Rally" }, Ct);

        var created = await s.Teams.CreateAsync(NewTeam("  Ferrari "), Ct);
        created.IsSuccess.ShouldBeTrue();
        (await s.Teams.GetAsync(created.Value, Ct))!.Name.ShouldBe("Ferrari");

        ShouldFailWith(
            (await s.Teams.CreateAsync(NewTeam("FERRARI", "f1", "FR"), Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Name)
        );
        (await s.Teams.CreateAsync(NewTeam("Ferrari", "Rally"), Ct)).IsSuccess.ShouldBeTrue();
        ShouldFailWith(
            (await s.Teams.CreateAsync(NewTeam(sport: "Nope"), Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Sport)
        );
        ShouldFailWith(
            (await s.Teams.CreateAsync(NewTeam("X", country: "ZZ"), Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Country)
        );
        ShouldFailWith(
            (await s.Teams.CreateAsync(NewTeam(new string('a', 51)), Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Name)
        );
        (
            await s.Teams.GetAsync((await s.Teams.CreateAsync(NewTeam("Canon", "f1"), Ct)).Value, Ct)
        )!.Sport.Value.ShouldBe("F1");
    }

    [Fact]
    public async Task Team_edit_allows_country_change_but_not_sport_change()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        await s.Sports.CreateAsync(new SportForm { Name = "Rally" }, Ct);
        var id = (await s.Teams.CreateAsync(NewTeam(), Ct)).Value;
        var other = (await s.Teams.CreateAsync(NewTeam("Mercedes"), Ct)).Value;

        var form = (await s.Teams.GetFormAsync(id, Ct))!;
        form.Country = "FR";
        form.Name = "Scuderia";
        (await s.Teams.UpdateAsync(id, form, Ct)).IsSuccess.ShouldBeTrue();
        var info = (await s.Teams.GetAsync(id, Ct))!;
        info.Country.Value.ShouldBe("FR");
        info.Name.ShouldBe("Scuderia");

        var changeSport = (await s.Teams.GetFormAsync(id, Ct))!;
        changeSport.Sport = "Rally";
        ShouldFailWith(
            (await s.Teams.UpdateAsync(id, changeSport, Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Sport)
        );

        var clash = (await s.Teams.GetFormAsync(other, Ct))!;
        clash.Name = "scuderia";
        ShouldFailWith(
            (await s.Teams.UpdateAsync(other, clash, Ct)).Error,
            ErrorKind.Validation,
            nameof(TeamForm.Name)
        );
        var self = (await s.Teams.GetFormAsync(id, Ct))!;
        self.Name = "SCUDERIA";
        (await s.Teams.UpdateAsync(id, self, Ct)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Team_update_reports_stale_forms_and_not_found()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        var id = (await s.Teams.CreateAsync(NewTeam(), Ct)).Value;
        var stale = (await s.Teams.GetFormAsync(id, Ct))!;
        var fresh = (await s.Teams.GetFormAsync(id, Ct))!;
        fresh.Country = "DE";
        await s.Teams.UpdateAsync(id, fresh, Ct);

        stale.Name = "Mine";
        ShouldFailWith((await s.Teams.UpdateAsync(id, stale, Ct)).Error, ErrorKind.Conflict);
        (await s.Teams.GetAsync(id, Ct))!.Name.ShouldBe("Ferrari");
        ShouldFailWith((await s.Teams.UpdateAsync(9999, stale, Ct)).Error, ErrorKind.NotFound);
        (await s.Teams.GetAsync(9999, Ct)).ShouldBeNull();
        ShouldFailWith((await s.Teams.DeleteAsync(9999, Ct)).Error, ErrorKind.NotFound);
    }

    [Fact]
    public async Task Team_delete_is_blocked_by_season_entries_and_allowed_otherwise()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        var used = (await s.Teams.CreateAsync(NewTeam("Used"), Ct)).Value;
        var free = (await s.Teams.CreateAsync(NewTeam("Free"), Ct)).Value;
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Season` (Sport, Label) VALUES ('F1','2020');"
                + $"INSERT INTO `SeasonEntry` (Season, Team, Name) VALUES (1,{used},'Used')"
        );

        ShouldFailWith((await s.Teams.DeleteAsync(used, Ct)).Error, ErrorKind.Conflict);
        (await s.Teams.GetAsync(used, Ct))!.SeasonEntryCount.ShouldBe(1);

        (await s.Teams.DeleteAsync(free, Ct)).IsSuccess.ShouldBeTrue();
        (await s.Teams.GetAsync(free, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Team_delete_blocked_by_other_references_is_a_conflict_not_an_exception()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);
        var team = (await s.Teams.CreateAsync(NewTeam(), Ct)).Value;
        await fixture.ExecuteSqlAsync($"INSERT INTO `Season` (Sport, Label, WinningTeam) VALUES ('F1','2020',{team})");

        var result = await s.Teams.DeleteAsync(team, Ct);

        ShouldFailWith(result.Error, ErrorKind.Conflict);
        (await s.Teams.GetAsync(team, Ct)).ShouldNotBeNull();
    }

    // ---------- Participants ----------

    [Fact]
    public async Task Participant_create_trims_validates_and_rejects_duplicates()
    {
        var s = await StartAsync();
        var ok = await s.Participants.CreateAsync(
            new ParticipantForm
            {
                Title = " Sir ",
                FirstName = " Lewis ",
                LastName = " Hamilton ",
                Country = "gb",
            },
            Ct
        );
        ok.IsSuccess.ShouldBeTrue();
        var info = (await s.Participants.GetAsync(ok.Value, Ct))!;
        (info.Title, info.FirstName, info.LastName, info.Country.Value).ShouldBe(("Sir", "Lewis", "Hamilton", "GB"));
        info.Sports.ShouldBeEmpty();

        var duplicate = await s.Participants.CreateAsync(
            new ParticipantForm
            {
                Title = "SIR",
                FirstName = "LEWIS",
                LastName = "hamilton",
                Country = "GB",
            },
            Ct
        );
        ShouldFailWith(duplicate.Error, ErrorKind.Validation);
        (
            await s.Participants.CreateAsync(
                new ParticipantForm
                {
                    FirstName = "Lewis",
                    LastName = "Hamilton",
                    Country = "GB",
                },
                Ct
            )
        ).IsSuccess.ShouldBeTrue();
        (
            await s.Participants.CreateAsync(
                new ParticipantForm
                {
                    Title = "Sir",
                    FirstName = "Lewis",
                    LastName = "Hamilton",
                    Country = "FR",
                },
                Ct
            )
        ).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Participant_details_list_distinct_sports_from_round_and_season_wins()
    {
        var s = await StartAsync();
        var participantId = (
            await s.Participants.CreateAsync(
                new ParticipantForm
                {
                    FirstName = "Alex",
                    LastName = "Driver",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Sport` (Name) VALUES ('F1'), ('MotoGP');"
                + "INSERT INTO `Season` (Sport, Label) VALUES ('F1', '2025'), ('MotoGP', '2025'), ('F1', '2024');"
                + $"INSERT INTO `SeasonWinner` (Season, Participant) VALUES (1, {participantId}), (2, {participantId}), (3, {participantId}), (1, {participantId});"
                + "INSERT INTO `Venue` (Name, Country) VALUES ('Silverstone', 'GB');"
                + "INSERT INTO `Round` (`Date`, Number, Season, Venue, Status) "
                + "VALUES ('2025-07-01', 1, 1, 'Silverstone', 'Scheduled');"
                + $"INSERT INTO `RoundWinner` (Round, Participant) VALUES (1, {participantId});"
        );

        var info = (await s.Participants.GetAsync(participantId, Ct))!;
        info.Sports.Select(sportName => sportName.Value).ShouldBe(["F1", "MotoGP"]);
        info.RoundWins.Select(win => win.RoundId.Value).ShouldBe([1]);
        info.RoundWins[0].Venue.Value.ShouldBe("Silverstone");
        info.RoundWins[0].VenueCountry.Value.ShouldBe("GB");
        info.RoundWins[0].Rating.ShouldBeNull();
        info.SeasonWins.Select(win => win.SeasonId.Value).ShouldBe([1, 3, 2]);
        info.SeasonWins.Single(win => win.SeasonId.Value == 1).StartDate.ShouldBe(new DateOnly(2025, 7, 1));
        info.SeasonWins.Single(win => win.SeasonId.Value == 1).EndDate.ShouldBe(new DateOnly(2025, 7, 1));
        info.SeasonWins.Single(win => win.SeasonId.Value == 2).StartDate.ShouldBeNull();
    }

    [Fact]
    public async Task Participant_create_rejects_invalid_country_and_over_long_values()
    {
        var s = await StartAsync();
        var bad = new[]
        {
            (
                new ParticipantForm
                {
                    FirstName = "A",
                    LastName = "B",
                    Country = "ZZ",
                },
                nameof(ParticipantForm.Country)
            ),
            (
                new ParticipantForm
                {
                    Title = new string('t', 11),
                    FirstName = "A",
                    LastName = "B",
                    Country = "GB",
                },
                nameof(ParticipantForm.Title)
            ),
            (
                new ParticipantForm
                {
                    FirstName = new string('a', 101),
                    LastName = "B",
                    Country = "GB",
                },
                nameof(ParticipantForm.FirstName)
            ),
            (
                new ParticipantForm
                {
                    FirstName = "A",
                    LastName = new string('b', 101),
                    Country = "GB",
                },
                nameof(ParticipantForm.LastName)
            ),
            (
                new ParticipantForm
                {
                    FirstName = " ",
                    LastName = "B",
                    Country = "GB",
                },
                nameof(ParticipantForm.FirstName)
            ),
        };
        foreach (var (form, field) in bad)
        {
            ShouldFailWith((await s.Participants.CreateAsync(form, Ct)).Error, ErrorKind.Validation, field);
        }

        (await CountAsync("SELECT 1 FROM `Participant`")).ShouldBe(0);
    }

    [Fact]
    public async Task Participant_list_is_sorted_by_last_name_first_name_country_and_shows_nice_names()
    {
        var s = await StartAsync();
        await fixture.ExecuteSqlAsync(
            "INSERT INTO `Participant` (Title, FirstName, LastName, Country) VALUES "
                + "('','Zed','Smith','GB'),('','Amy','Smith','GB'),('','Amy','Smith','FR'),('','Bob','Adams','DE')"
        );

        var page = await s.Participants.ListAsync(null, 1, Ct);

        page.Items.Select(p => $"{p.LastName} {p.FirstName} {p.CountryName}")
            .ToArray()
            .ShouldBe([
                "Adams Bob Germany",
                "Smith Amy France",
                "Smith Amy United Kingdom",
                "Smith Zed United Kingdom",
            ]);
        (await s.Participants.ListAsync("zed", 1, Ct)).Total.ShouldBe(1);
    }

    [Fact]
    public async Task Participant_update_reports_duplicates_stale_forms_and_not_found()
    {
        var s = await StartAsync();
        var a = (
            await s.Participants.CreateAsync(
                new ParticipantForm
                {
                    FirstName = "A",
                    LastName = "X",
                    Country = "GB",
                },
                Ct
            )
        ).Value;
        await s.Participants.CreateAsync(
            new ParticipantForm
            {
                FirstName = "B",
                LastName = "X",
                Country = "GB",
            },
            Ct
        );
        var stale = (await s.Participants.GetFormAsync(a, Ct))!;
        var form = (await s.Participants.GetFormAsync(a, Ct))!;
        form.Country = "FR";
        (await s.Participants.UpdateAsync(a, form, Ct)).IsSuccess.ShouldBeTrue();

        stale.FirstName = "Mine";
        ShouldFailWith((await s.Participants.UpdateAsync(a, stale, Ct)).Error, ErrorKind.Conflict);

        var dup = (await s.Participants.GetFormAsync(a, Ct))!;
        dup.FirstName = "b";
        dup.Country = "GB";
        ShouldFailWith((await s.Participants.UpdateAsync(a, dup, Ct)).Error, ErrorKind.Validation);
        ShouldFailWith((await s.Participants.UpdateAsync(9999, dup, Ct)).Error, ErrorKind.NotFound);
        (await s.Participants.GetAsync(9999, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Concurrent_duplicate_creates_yield_one_row_and_no_exception()
    {
        var s = await StartAsync();
        await s.Sports.CreateAsync(new SportForm { Name = "F1" }, Ct);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(_ => s.Teams.CreateAsync(NewTeam("Race"), Ct)).Concat([]).ToArray()
        );

        results.Count(r => r.IsSuccess).ShouldBe(1);
        (await CountAsync("SELECT 1 FROM `Team`")).ShouldBe(1);
    }
}
