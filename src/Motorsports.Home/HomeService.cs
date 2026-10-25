using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;

namespace Motorsports.Home;

internal sealed class HomeService(IDbContextFactory<MotorsportsDbContext> factory)
{
    public async Task<HomeOverview> GetHomeOverviewAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var nextUp = await db
            .Rounds.AsNoTracking()
            .Where(round => round.StatusRef!.Step < 2)
            .Where(round =>
                !db.Rounds.Any(other =>
                    other.StatusRef!.Step < 2
                    && other.SeasonRef!.Sport == round.SeasonRef!.Sport
                    && (other.Date < round.Date || (other.Date == round.Date && other.Id < round.Id))
                )
            )
            .OrderBy(round => round.Date)
            .ThenBy(round => round.SeasonRef!.Sport)
            .Select(round => new HomeRound(
                round.Id,
                round.SeasonRef!.Sport,
                round.Season,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                db.Venues.Where(venue => venue.Name == round.Venue).Select(venue => venue.Country).FirstOrDefault()!,
                Array.Empty<HomeHistoryItem>()
            ))
            .ToArrayAsync(cancellationToken);

        var veryNextUp = nextUp
            .Where(round => round.Date <= today)
            .OrderBy(round => round.Date)
            .ThenBy(round => round.Sport, StringComparer.Ordinal)
            .ThenBy(round => round.Id)
            .FirstOrDefault();

        var nextUpBySport = nextUp.Where(round => round.Id != veryNextUp?.Id).ToArray();

        var roundsToAcquire = await db
            .Rounds.AsNoTracking()
            .Where(round => round.StatusRef!.Step == 0 && round.Date <= today)
            .OrderBy(round => round.Date)
            .ThenBy(round => round.SeasonRef!.Sport)
            .ThenBy(round => round.Id)
            .Select(round => new HomeRound(
                round.Id,
                round.SeasonRef!.Sport,
                round.Season,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                db.Venues.Where(venue => venue.Name == round.Venue).Select(venue => venue.Country).FirstOrDefault()!,
                Array.Empty<HomeHistoryItem>()
            ))
            .ToArrayAsync(cancellationToken);

        var histories =
            nextUpBySport.Length == 0
                ? new Dictionary<int, HomeHistoryItem[]>()
                : await GetEventHistoryAsync(db, nextUpBySport, cancellationToken);

        nextUpBySport = nextUpBySport
            .Select(round => round with { EventHistory = histories.GetValueOrDefault(round.Id, []) })
            .ToArray();
        return new HomeOverview(veryNextUp, nextUpBySport, roundsToAcquire);
    }

    private static async Task<Dictionary<int, HomeHistoryItem[]>> GetEventHistoryAsync(
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
                    .Select(team => team.Country)
                    .FirstOrDefault(),
            })
            .ToArrayAsync(cancellationToken);

        var relevantRows = rows.Where(row =>
                selectedRounds.Any(selected =>
                    selected.Sport == row.Sport
                    && selected.Venue == row.Venue
                    && row.Date >= selected.Date.AddYears(-10)
                    && row.Date <= selected.Date
                    && (selected.Number == 0 ? row.Number == 0 : row.Number > 0)
                )
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
                Label = CountryFlag.WithName(winner.Country, winner.FirstName + " " + winner.LastName),
            })
            .ToArrayAsync(cancellationToken);
        var winnersByRound = winners
            .GroupBy(winner => winner.Round)
            .ToDictionary(group => group.Key, group => group.Select(winner => winner.Label).ToArray());

        return selectedRounds.ToDictionary(
            selected => selected.Id,
            selected =>
                relevantRows
                    .Where(row =>
                        row.Sport == selected.Sport
                        && row.Venue == selected.Venue
                        && row.Date >= selected.Date.AddYears(-10)
                        && row.Date <= selected.Date
                        && (selected.Number == 0 ? row.Number == 0 : row.Number > 0)
                    )
                    .OrderByDescending(row => row.Date)
                    .ThenByDescending(row => row.Id)
                    .Take(50)
                    .Select(row => new HomeHistoryItem(
                        row.Id,
                        row.Date,
                        row.Number,
                        row.Name,
                        row.Rating,
                        row.Rain,
                        row.WinningTeamName is null
                            ? null
                            : CountryFlag.WithName(row.WinningTeamCountry, row.WinningTeamName),
                        winnersByRound.GetValueOrDefault(row.Id) ?? []
                    ))
                    .ToArray()
        );
    }
}
