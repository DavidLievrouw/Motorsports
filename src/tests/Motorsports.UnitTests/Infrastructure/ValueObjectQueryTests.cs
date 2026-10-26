using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Shouldly;

namespace Motorsports.UnitTests.Infrastructure;

public sealed class ValueObjectQueryTests
{
    [Fact]
    public void Converted_key_membership_ordering_and_projection_translate()
    {
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, "Server=localhost;Database=x");
        using var db = new MotorsportsDbContext(options.Options);

        VenueName[] names = [VenueName.From("Spa"), VenueName.From("Silverstone")];
        var query = db
            .Venues.Where(venue => names.Contains(venue.Name))
            .OrderBy(venue => venue.Name)
            .Select(venue => new { venue.Name, Country = venue.Country.Value })
            .ToQueryString();

        query.ShouldContain("IN");
        query.ShouldContain("ORDER BY");
    }

    [Fact]
    public void Earliest_rounds_by_sport_translate_to_sql()
    {
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, "Server=localhost;Database=x");
        using var db = new MotorsportsDbContext(options.Options);

        var earliestDates = db
            .Rounds.AsNoTracking()
            .Where(round => round.StatusRef!.Step < 2)
            .GroupBy(round => round.SeasonRef!.Sport)
            .Select(group => new { Sport = group.Key, Date = group.Min(round => round.Date) });
        var query = earliestDates
            .Join(
                db.Rounds.AsNoTracking(),
                earliest => new { earliest.Sport, earliest.Date },
                round => new { Sport = round.SeasonRef!.Sport, round.Date },
                (_, round) => round
            )
            .OrderBy(round => round.Date)
            .ThenBy(round => round.SeasonRef!.Sport)
            .ThenBy(round => round.Id)
            .Select(round => round.Id)
            .ToQueryString();

        query.ShouldContain("MIN(");
        query.ShouldContain("JOIN");
        query.ShouldContain("ORDER BY");
    }

    [Fact]
    public void Team_and_participant_searches_translate_without_unwrapping_value_objects()
    {
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, "Server=localhost;Database=x");
        using var db = new MotorsportsDbContext(options.Options);
        var search = "F1";

        var matchingSports = db
            .Sports.AsNoTracking()
            .Where(sport =>
                sport.Name == SportName.From(search) || (sport.FullName != null && sport.FullName.Contains(search))
            )
            .Select(sport => sport.Name);
        var teams = db
            .Teams.AsNoTracking()
            .Where(team =>
                team.Name.Contains(search)
                || matchingSports.Contains(team.Sport)
                || (team.CountryRef!.NiceName != null && team.CountryRef.NiceName.Contains(search))
                || (team.CountryRef.Name != null && team.CountryRef.Name.Contains(search))
            )
            .Union(db.Teams.AsNoTracking().Where(team => team.Country == CountryIso.From(search)))
            .ToQueryString();
        var participants = db
            .Participants.AsNoTracking()
            .Where(participant =>
                participant.FirstName.Contains(search)
                || participant.LastName.Contains(search)
                || (participant.CountryRef!.NiceName != null && participant.CountryRef.NiceName.Contains(search))
                || (participant.CountryRef.Name != null && participant.CountryRef.Name.Contains(search))
            )
            .Union(db.Participants.AsNoTracking().Where(participant => participant.Country == CountryIso.From(search)))
            .ToQueryString();

        teams.ShouldContain("UNION");
        teams.ShouldContain("LIKE");
        participants.ShouldContain("UNION");
        participants.ShouldContain("LIKE");
    }
}
