using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.PlaywrightTests;

/// <summary>Synthetic rows that pickers search. They exist before the application starts, so the lookup cache is warm and stable.</summary>
public static class PickerData
{
    public const string AliasTeam = "Picker Alias Team";
    public const string OtherTeam = "Picker Other Team";
    public const string MotoTeam = "Picker Moto Team";
    public const string OldSeason = "PWP-A";
    public const string CurrentSeason = "PWP-B";
    public const string EmptySeason = "PWP-C";
    public const string LegacyEntry = "Legacy Racing";
    public const string ModernEntry = "Modern Racing";
    public const string OtherEntry = "Other Racing";
    public const string MotoSport = "MotoGP";
    public const string PagingVenuePrefix = "PWPAGE";
    public const int PagingVenueCount = 55;
}

public sealed partial class PlaywrightSuiteFixture
{
    public async Task<string> CreateVenueAsync(string namePrefix = "PW Venue")
    {
        var name = $"{namePrefix} {Guid.NewGuid():N}"[..Math.Min(namePrefix.Length + 9, 100)];
        await using var context = CreateContext();
        context.Venues.Add(new Venue { Name = VenueName.From(name), Country = CountryIso.From("GB") });
        await context.SaveChangesAsync();
        return name;
    }

    public async Task<int> CreateTeamAsync(string name, string sport = "F1")
    {
        await using var context = CreateContext();
        var team = new Team
        {
            Name = name,
            Sport = SportName.From(sport),
            Country = CountryIso.From("GB"),
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        return team.Id.Value;
    }

    public async Task<int> CreateSeasonAsync(string label)
    {
        await using var context = CreateContext();
        var season = new Season { Sport = SportName.F1, Label = label };
        context.Seasons.Add(season);
        await context.SaveChangesAsync();
        return season.Id.Value;
    }

    public async Task<int> AddEntryAsync(int seasonId, int teamId, string name)
    {
        await using var context = CreateContext();
        context.SeasonEntries.Add(
            new SeasonEntry
            {
                Season = SeasonId.From(seasonId),
                Team = TeamId.From(teamId),
                Name = name,
            }
        );
        await context.SaveChangesAsync();
        return teamId;
    }

    public async Task<int> CreateRoundAsync(
        int seasonId,
        DateOnly date,
        string name,
        string status = "Scheduled",
        int? winningTeam = null,
        params int[] winners
    )
    {
        await using var context = CreateContext();
        var round = new Round
        {
            Date = date,
            Number = 0,
            Name = name,
            Season = SeasonId.From(seasonId),
            Venue = VenueName.From("Silverstone"),
            Status = StatusName.From(status),
            WinningTeam = winningTeam is { } team ? TeamId.From(team) : null,
        };
        context.Rounds.Add(round);
        await context.SaveChangesAsync();
        foreach (var participant in winners)
        {
            context.RoundWinners.Add(
                new RoundWinner { Round = round.Id, Participant = ParticipantId.From(participant) }
            );
        }

        await context.SaveChangesAsync();
        return round.Id.Value;
    }

    public async Task<int> FindSeasonIdAsync(string label)
    {
        await using var context = CreateContext();
        return (await context.Seasons.AsNoTracking().SingleAsync(season => season.Label == label)).Id.Value;
    }

    public async Task<int> FindTeamIdAsync(string name)
    {
        await using var context = CreateContext();
        return (await context.Teams.AsNoTracking().SingleAsync(team => team.Name == name)).Id.Value;
    }

    public async Task<int> FindParticipantIdAsync(string firstName, string lastName)
    {
        await using var context = CreateContext();
        return (
            await context
                .Participants.AsNoTracking()
                .FirstAsync(participant => participant.FirstName == firstName && participant.LastName == lastName)
        )
            .Id
            .Value;
    }

    public async Task<bool> RoundExistsAsync(int roundId)
    {
        await using var context = CreateContext();
        return await context.Rounds.AnyAsync(round => round.Id == RoundId.From(roundId));
    }

    public async Task DeleteRoundAsync(int roundId)
    {
        await using var context = CreateContext();
        await context.Rounds.Where(round => round.Id == RoundId.From(roundId)).ExecuteDeleteAsync();
    }

    public async Task<bool> SeasonExistsAsync(int seasonId)
    {
        await using var context = CreateContext();
        return await context.Seasons.AnyAsync(season => season.Id == SeasonId.From(seasonId));
    }

    public async Task<bool> TeamExistsAsync(int teamId)
    {
        await using var context = CreateContext();
        return await context.Teams.AnyAsync(team => team.Id == TeamId.From(teamId));
    }

    public async Task<(string Status, int? Rating, int? Rain, string? Note, int Winners)> ReadRoundAsync(int roundId)
    {
        await using var context = CreateContext();
        var id = RoundId.From(roundId);
        var round = await context.Rounds.AsNoTracking().SingleAsync(r => r.Id == id);
        var winners = await context.RoundWinners.CountAsync(w => w.Round == id);
        return (round.Status.Value, (int?)round.Rating, (int?)round.Rain, round.Note, winners);
    }

    private static async Task SeedPickerDataAsync(MotorsportsDbContext context)
    {
        context.Countries.Add(
            new Country
            {
                Iso = CountryIso.From("NL"),
                Name = "NETHERLANDS",
                NiceName = "Netherlands",
                Iso3 = "NLD",
            }
        );
        context.Sports.Add(new Sport { Name = SportName.From(PickerData.MotoSport), FullName = "Moto GP" });
        for (var index = 1; index <= PickerData.PagingVenueCount; index++)
        {
            context.Venues.Add(
                new Venue
                {
                    Name = VenueName.From($"{PickerData.PagingVenuePrefix} {index:000}"),
                    Country = CountryIso.From("GB"),
                }
            );
        }

        await context.SaveChangesAsync();

        var gb = CountryIso.From("GB");
        var nl = CountryIso.From("NL");
        (string First, string Last, CountryIso Country)[] people =
        [
            ("Max", "Verstappen", nl),
            ("Lando", "Norris", gb),
            ("Zoë", "Müller", gb),
            ("Sam", "Smith", gb),
            ("Sam", "Smith", nl),
            ("Zed", "Pickeralpha", gb),
            ("Zed", "Pickerbeta", gb),
            ("Zed", "Pickergamma", gb),
            ("Zed", "Pickerdelta", gb),
            ("Zed", "Pickerepsilon", gb),
        ];
        foreach (var (first, last, country) in people)
        {
            context.Participants.Add(
                new Participant
                {
                    Title = "",
                    FirstName = first,
                    LastName = last,
                    Country = country,
                }
            );
        }

        var alias = new Team
        {
            Name = PickerData.AliasTeam,
            Sport = SportName.F1,
            Country = gb,
        };
        var other = new Team
        {
            Name = PickerData.OtherTeam,
            Sport = SportName.F1,
            Country = gb,
        };
        context.Teams.AddRange(
            alias,
            other,
            new Team
            {
                Name = PickerData.MotoTeam,
                Sport = SportName.From(PickerData.MotoSport),
                Country = gb,
            }
        );
        var seasonA = new Season { Sport = SportName.F1, Label = PickerData.OldSeason };
        var seasonB = new Season { Sport = SportName.F1, Label = PickerData.CurrentSeason };
        var seasonC = new Season { Sport = SportName.F1, Label = PickerData.EmptySeason };
        context.Seasons.AddRange(seasonA, seasonB, seasonC);
        await context.SaveChangesAsync();

        context.SeasonEntries.AddRange(
            new SeasonEntry
            {
                Season = seasonA.Id,
                Team = alias.Id,
                Name = PickerData.LegacyEntry,
            },
            new SeasonEntry
            {
                Season = seasonB.Id,
                Team = alias.Id,
                Name = PickerData.ModernEntry,
            },
            new SeasonEntry
            {
                Season = seasonB.Id,
                Team = other.Id,
                Name = PickerData.OtherEntry,
            }
        );
        await context.SaveChangesAsync();
    }
}
