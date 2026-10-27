using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;

namespace Motorsports.Home;

internal sealed class HomeService(IDbContextFactory<MotorsportsDbContext> factory)
{
    public async Task<HomeOverview> GetHomeOverviewAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var nextUpDates = db
            .Rounds.AsNoTracking()
            .Where(round => round.StatusRef!.Step < 2)
            .GroupBy(round => round.SeasonRef!.Sport)
            .Select(group => new { Sport = group.Key, Date = group.Min(round => round.Date) });
        var nextUpCandidates = await nextUpDates
            .Join(
                db.Rounds.AsNoTracking(),
                earliest => new { earliest.Sport, earliest.Date },
                round => new { Sport = round.SeasonRef!.Sport, round.Date },
                (_, round) => round
            )
            .Join(
                db.Venues.AsNoTracking(),
                round => round.Venue,
                venue => venue.Name,
                (round, venue) => new { Round = round, VenueCountry = venue.Country }
            )
            .OrderBy(row => row.Round.Date)
            .ThenBy(row => row.Round.SeasonRef!.Sport)
            .ThenBy(row => row.Round.Id)
            .Select(row => new HomeRound(
                row.Round.Id,
                row.Round.SeasonRef!.Sport,
                row.Round.Season,
                row.Round.Date,
                row.Round.Number,
                row.Round.Name,
                row.Round.Venue,
                row.VenueCountry,
                Array.Empty<HomeHistoryItem>()
            ))
            .ToArrayAsync(cancellationToken);
        var nextUp = nextUpCandidates
            .GroupBy(round => round.Sport)
            .Select(group => group.OrderBy(round => round.Id.Value).First())
            .ToArray();

        var veryNextUp = nextUp
            .Where(round => round.Date <= today)
            .OrderBy(round => round.Date)
            .ThenBy(round => round.Sport.Value, StringComparer.Ordinal)
            .ThenBy(round => round.Id.Value)
            .FirstOrDefault();

        var nextUpBySport = nextUp.Where(round => round.Id != veryNextUp?.Id).ToArray();

        var roundsToAcquire = await db
            .Rounds.AsNoTracking()
            .Where(round => round.StatusRef!.Step == 0 && round.Date <= today)
            .Join(
                db.Venues.AsNoTracking(),
                round => round.Venue,
                venue => venue.Name,
                (round, venue) => new { Round = round, VenueCountry = venue.Country }
            )
            .OrderBy(row => row.Round.Date)
            .ThenBy(row => row.Round.SeasonRef!.Sport)
            .ThenBy(row => row.Round.Id)
            .Select(row => new HomeRound(
                row.Round.Id,
                row.Round.SeasonRef!.Sport,
                row.Round.Season,
                row.Round.Date,
                row.Round.Number,
                row.Round.Name,
                row.Round.Venue,
                row.VenueCountry,
                Array.Empty<HomeHistoryItem>()
            ))
            .ToArrayAsync(cancellationToken);

        var histories =
            nextUpBySport.Length == 0
                ? new Dictionary<RoundId, HomeHistoryItem[]>()
                : await GetEventHistoryAsync(db, nextUpBySport, cancellationToken);

        nextUpBySport = nextUpBySport
            .Select(round => round with { EventHistory = histories.GetValueOrDefault(round.Id, []) })
            .ToArray();
        return new HomeOverview(veryNextUp, nextUpBySport, roundsToAcquire);
    }

    private static async Task<Dictionary<RoundId, HomeHistoryItem[]>> GetEventHistoryAsync(
        MotorsportsDbContext db,
        HomeRound[] selectedRounds,
        CancellationToken cancellationToken
    )
    {
        var selectedIds = selectedRounds.Select(round => round.Id).ToArray();
        var sports = selectedRounds.Select(round => round.Sport).Distinct().ToArray();
        var venues = selectedRounds.Select(round => round.Venue).Distinct().ToArray();
        var earliestDate = selectedRounds.Min(round => round.Date.AddYears(-10));
        var latestDate = selectedRounds.Max(round => round.Date);
        var rows = await db
            .Rounds.AsNoTracking()
            .Where(round =>
                !selectedIds.Contains(round.Id)
                && venues.Contains(round.Venue)
                && sports.Contains(round.SeasonRef!.Sport)
                && round.StatusRef!.Step > 1
                && round.Date >= earliestDate
                && round.Date <= latestDate
            )
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.Id)
            .Select(round => new
            {
                round.Id,
                Sport = round.SeasonRef!.Sport,
                round.Venue,
                round.Date,
                round.Number,
                round.Name,
                round.Rating,
                round.Rain,
                WinningTeamName = round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry => entry.Season == round.Season && entry.Team == round.WinningTeam)
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == round.WinningTeam)
                            .Select(team => team.Name)
                            .FirstOrDefault(),
                WinningTeamCountry = db
                    .Teams.Where(team => team.Id == round.WinningTeam)
                    .Select(team => (CountryIso?)team.Country)
                    .FirstOrDefault(),
            })
            .ToArrayAsync(cancellationToken);

        var relevantRows = rows.Where(row =>
                selectedRounds.Any(selected => MatchesHistory(selected, row.Sport, row.Venue, row.Date, row.Number))
            )
            .ToArray();
        if (relevantRows.Length == 0)
        {
            return [];
        }

        var roundIds = relevantRows.Select(row => row.Id).ToArray();
        var winners = await db
            .RoundWinners.AsNoTracking()
            .Where(winner => roundIds.Contains(winner.Round))
            .Join(
                db.Participants.AsNoTracking(),
                winner => winner.Participant,
                participant => participant.Id,
                (winner, participant) =>
                    new
                    {
                        winner.Round,
                        participant.FirstName,
                        participant.LastName,
                        participant.Country,
                    }
            )
            .OrderBy(winner => winner.LastName)
            .ThenBy(winner => winner.FirstName)
            .Select(winner => new
            {
                winner.Round,
                Label = CountryFlag.WithName(winner.Country.Value, winner.FirstName + " " + winner.LastName),
            })
            .ToArrayAsync(cancellationToken);
        var winnersByRound = winners
            .GroupBy(winner => winner.Round)
            .ToDictionary(group => group.Key, group => group.Select(winner => winner.Label).ToArray());

        return selectedRounds.ToDictionary(
            selected => selected.Id,
            selected =>
                relevantRows
                    .Where(row => MatchesHistory(selected, row.Sport, row.Venue, row.Date, row.Number))
                    .OrderByDescending(row => row.Date)
                    .ThenByDescending(row => row.Id.Value)
                    .Take(50)
                    .Select(row => new HomeHistoryItem(
                        row.Id,
                        row.Date,
                        row.Number,
                        row.Name,
                        row.Rating,
                        row.Rain,
                        FormatWinningTeam(row.WinningTeamName, row.WinningTeamCountry),
                        winnersByRound.GetValueOrDefault(row.Id) ?? []
                    ))
                    .ToArray()
        );
    }

    private static bool MatchesHistory(
        HomeRound selected,
        SportName sport,
        VenueName venue,
        DateOnly date,
        short number
    ) =>
        sport == selected.Sport
        && venue == selected.Venue
        && date >= selected.Date.AddYears(-10)
        && date <= selected.Date
        && (selected.Number == 0 ? number == 0 : number > 0);

    private static string? FormatWinningTeam(string? name, CountryIso? country)
    {
        if (name is null)
        {
            return null;
        }

        return country is { } iso ? CountryFlag.WithName(iso.Value, name) : name;
    }
}
