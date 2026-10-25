using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;

// ReSharper disable AccessToDisposedClosure

namespace Motorsports.Rounds;

internal sealed class RoundService(IDbContextFactory<MotorsportsDbContext> factory, IValidator<RoundForm> validator)
{
    public const string StaleMessage =
        "This round was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public const string HasWinnersMessage = "A round with winners or a winning team cannot be deleted.";

    public async Task<RoundSeasonContext?> GetSeasonAsync(int seasonId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .Seasons.AsNoTracking()
            .Where(season => season.Id == seasonId)
            .Select(season => new RoundSeasonContext(season.Id, season.Sport, season.Label))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoundRow>> ListAsync(int seasonId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var rows = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Season == seasonId)
            .OrderBy(round => round.Date)
            .ThenBy(round => round.Number)
            .ThenBy(round => round.Id)
            .Select(round => new
            {
                round.Id,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                VenueCountry = db
                    .Venues.Where(venue => venue.Name == round.Venue)
                    .Select(venue => venue.Country)
                    .FirstOrDefault(),
                round.Status,
                StatusStep = round.StatusRef!.Step,
                round.Rating,
                round.WinningTeam,
                TeamCountry = round.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == round.WinningTeam.Value)
                        .Select(team => team.Country)
                        .FirstOrDefault(),
                TeamName = round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry =>
                            entry.Season == round.Season && entry.Team == round.WinningTeam.Value
                        )
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == round.WinningTeam.Value)
                            .Select(team => team.Name)
                            .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(row => row.Id).ToArray();
        var winners = await LoadWinnersAsync(db, ids, cancellationToken);
        return
        [
            .. rows.Select(row => new RoundRow(
                row.Id,
                row.Date,
                row.Number,
                row.Name,
                row.Venue,
                row.VenueCountry ?? "",
                row.Status,
                row.StatusStep,
                row.Rating,
                row.TeamName,
                row.TeamCountry,
                winners.GetValueOrDefault(row.Id, [])
            )),
        ];
    }

    public async Task<RoundInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var round = await db
            .Rounds.AsNoTracking()
            .Where(row => row.Id == id)
            .Select(row => new
            {
                row.Id,
                row.Season,
                row.SeasonRef!.Sport,
                SeasonLabel = row.SeasonRef.Label,
                row.Date,
                row.Number,
                row.Name,
                row.Venue,
                VenueCountry = db
                    .Venues.Where(venue => venue.Name == row.Venue)
                    .Select(venue => venue.Country)
                    .FirstOrDefault(),
                row.Status,
                StatusStep = row.StatusRef!.Step,
                row.Rating,
                row.Rain,
                row.Note,
                row.WinningTeam,
                TeamCountry = row.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == row.WinningTeam.Value)
                        .Select(team => team.Country)
                        .FirstOrDefault(),
                TeamName = row.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry => entry.Season == row.Season && entry.Team == row.WinningTeam.Value)
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == row.WinningTeam.Value)
                            .Select(team => team.Name)
                            .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null)
        {
            return null;
        }

        var winners = await LoadWinnersAsync(db, [round.Id], cancellationToken);
        return new RoundInfo(
            round.Id,
            round.Season,
            round.Sport,
            round.SeasonLabel,
            round.Date,
            round.Number,
            round.Name,
            round.Venue,
            round.VenueCountry ?? "",
            round.Status,
            round.StatusStep,
            round.Rating,
            round.Rain,
            round.Note,
            round.WinningTeam,
            round.TeamName,
            round.TeamCountry,
            winners.GetValueOrDefault(round.Id, [])
        );
    }

    public async Task<IReadOnlyList<EventHistoryItem>> GetEventHistoryAsync(
        int roundId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var source = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == roundId)
            .Select(round => new
            {
                round.Id,
                round.Venue,
                round.Number,
                round.Date,
                round.SeasonRef!.Sport,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            return [];
        }

        var championship = source.Number > 0;
        var rows = await db
            .Rounds.AsNoTracking()
            .Where(round =>
                round.Id != source.Id
                && round.Venue == source.Venue
                && round.SeasonRef!.Sport == source.Sport
                && round.StatusRef!.Step > 1
                && round.Date <= source.Date
                && (championship ? round.Number > 0 : round.Number == 0)
            )
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.Id)
            .Take(50)
            .Select(round => new
            {
                round.Id,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                VenueCountry = db
                    .Venues.Where(venue => venue.Name == round.Venue)
                    .Select(venue => venue.Country)
                    .FirstOrDefault(),
                round.Status,
                round.Rating,
                round.Rain,
                round.WinningTeam,
                TeamCountry = round.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == round.WinningTeam.Value)
                        .Select(team => team.Country)
                        .FirstOrDefault(),
                TeamName = round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry =>
                            entry.Season == round.Season && entry.Team == round.WinningTeam.Value
                        )
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == round.WinningTeam.Value)
                            .Select(team => team.Name)
                            .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var winners = await LoadWinnersAsync(db, rows.Select(row => row.Id).ToArray(), cancellationToken);
        return
        [
            .. rows.Select(row => new EventHistoryItem(
                row.Id,
                row.Date,
                row.Number,
                row.Name,
                row.Venue,
                row.VenueCountry ?? "",
                row.Status,
                row.Rating,
                row.Rain,
                row.TeamName,
                row.TeamCountry,
                winners.GetValueOrDefault(row.Id, [])
            )),
        ];
    }

    public async Task<RoundForm?> GetCreateFormAsync(
        int seasonId,
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Seasons.AnyAsync(season => season.Id == seasonId, cancellationToken))
        {
            return null;
        }

        var last = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Season == seasonId)
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.Id)
            .Select(round => new { round.Date, round.Number })
            .FirstOrDefaultAsync(cancellationToken);
        var scheduled =
            await db
                .Statuses.AsNoTracking()
                .Where(status => status.Name == "Scheduled")
                .Select(status => status.Name)
                .FirstOrDefaultAsync(cancellationToken)
            ?? "Scheduled";
        var nextNumber = last is null ? (short)1 : (short)Math.Min(99, last.Number + 1);
        return new RoundForm
        {
            SeasonId = seasonId,
            Date = last?.Date ?? today,
            Number = nextNumber,
            Status = scheduled,
        };
    }

    public async Task<RoundForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == id)
            .Select(round => new
            {
                round.Season,
                round.Date,
                round.Number,
                round.Name,
                round.Venue,
                round.Status,
                round.Note,
                round.Rating,
                round.Rain,
                round.WinningTeam,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var winners = await db
            .RoundWinners.AsNoTracking()
            .Where(winner => winner.Round == id)
            .OrderBy(winner => winner.Participant)
            .Select(winner => winner.Participant)
            .ToListAsync(cancellationToken);
        return new RoundForm
        {
            SeasonId = row.Season,
            Date = row.Date,
            OriginalDate = row.Date,
            Number = row.Number,
            Name = row.Name,
            Venue = row.Venue,
            Status = row.Status,
            Note = row.Note,
            Rating = row.Rating is { } rating ? decimal.ToInt32(rating) : null,
            Rain = row.Rain is { } rain ? (RainLevel)(int)rain : null,
            WinningTeam = row.WinningTeam,
            Winners = winners,
            RowVersion = ComputeVersion(
                row.Season,
                row.Date,
                row.Number,
                row.Name,
                row.Venue,
                row.Status,
                row.Note,
                row.Rating,
                row.Rain,
                row.WinningTeam,
                winners
            ),
        };
    }

    public async Task<Result<int>> CreateAsync(RoundForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                if (!await db.Seasons.AnyAsync(season => season.Id == form.SeasonId, ct))
                {
                    return Error.NotFound("Season not found.");
                }

                var lookup = await ResolveLookupsAsync(db, form, excludeRoundId: null, ct);
                if (lookup.Error is { } error)
                {
                    return error;
                }

                var entity = new Round
                {
                    Season = form.SeasonId,
                    Date = form.Date!.Value,
                    Number = form.Number,
                    Name = BlankToNull(form.Name),
                    Venue = lookup.Venue!,
                    Status = lookup.Status!,
                    Note = BlankToNull(form.Note),
                };
                db.Rounds.Add(entity);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(entity.Id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        return result;
    }

    public async Task<Result<int>> UpdateAsync(int id, RoundForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Round not found.");
                }

                var current = rows[0];
                if (current.Season != form.SeasonId)
                {
                    return Error.Validation(nameof(RoundForm.SeasonId), "The season of a round cannot be changed.");
                }

                var winnerIds = await db
                    .RoundWinners.Where(winner => winner.Round == id)
                    .Select(winner => winner.Participant)
                    .ToListAsync(ct);
                if (
                    form.RowVersion
                    != ComputeVersion(
                        current.Season,
                        current.Date,
                        current.Number,
                        current.Name,
                        current.Venue,
                        current.Status,
                        current.Note,
                        current.Rating,
                        current.Rain,
                        current.WinningTeam,
                        winnerIds
                    )
                )
                {
                    return Error.Conflict(StaleMessage);
                }

                var lookup = await ResolveLookupsAsync(db, form, id, ct);
                if (lookup.Error is { } error)
                {
                    return error;
                }

                var rating = (decimal?)form.Rating;
                if (lookup.StatusStep <= 1 && (rating is null || rating <= 0))
                {
                    rating = null;
                }

                decimal? rainValue = form.Rain.HasValue ? (int)form.Rain.Value : null;
                var name = BlankToNull(form.Name);
                var note = BlankToNull(form.Note);
                var date = form.Date!.Value;
                var venue = lookup.Venue;
                var status = lookup.Status;
                var winningTeam = form.WinningTeam;
                var number = form.Number;

                await db
                    .Rounds.Where(round => round.Id == id)
                    .ExecuteUpdateAsync(
                        updates =>
                            updates
                                .SetProperty(round => round.Date, date)
                                .SetProperty(round => round.Number, number)
                                .SetProperty(round => round.Name, name)
                                .SetProperty(round => round.Venue, venue)
                                .SetProperty(round => round.Status, status)
                                .SetProperty(round => round.Note, note)
                                .SetProperty(round => round.Rating, rating)
                                .SetProperty(round => round.Rain, rainValue)
                                .SetProperty(round => round.WinningTeam, winningTeam),
                        ct
                    );
                await db.RoundWinners.Where(winner => winner.Round == id).ExecuteDeleteAsync(ct);
                if (form.Winners.Count > 0)
                {
                    db.RoundWinners.AddRange(
                        form.Winners.Select(participant => new RoundWinner { Round = id, Participant = participant })
                    );
                    await db.SaveChangesAsync(ct);
                }

                return Result<int>.Ok(id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        return result;
    }

    public async Task<RoundDeleteInfo?> GetDeleteInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == id)
            .Select(round => new
            {
                round.Id,
                round.Season,
                round.Number,
                round.Name,
                HasWinners = db.RoundWinners.Any(winner => winner.Round == id),
                HasWinningTeam = round.WinningTeam != null,
            })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new RoundDeleteInfo(
                row.Id,
                row.Season,
                RoundLabel.Format(row.Number, row.Name),
                row.HasWinners,
                row.HasWinningTeam
            );
    }

    public async Task<Result<Unit>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Round not found.");
                }

                var current = rows[0];
                if (current.WinningTeam is not null || await db.RoundWinners.AnyAsync(winner => winner.Round == id, ct))
                {
                    return Error.Conflict(HasWinnersMessage);
                }

                await db.Rounds.Where(round => round.Id == id).ExecuteDeleteAsync(ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
    }

    public async Task<Result<Unit>> MarkReadyToWatchAsync(int id, CancellationToken cancellationToken = default)
    {
        return await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Round not found.");
                }

                var current = rows[0];
                var step = await db
                    .Statuses.Where(status => status.Name == current.Status)
                    .Select(status => (sbyte?)status.Step)
                    .FirstOrDefaultAsync(ct);
                if (step is null || step >= 1)
                {
                    return Result.Ok();
                }

                var ready = await db
                    .Statuses.Where(status => status.Name == "ReadyToWatch")
                    .Select(status => status.Name)
                    .FirstOrDefaultAsync(ct);
                if (ready is null)
                {
                    return Error.Validation(nameof(RoundForm.Status), "Status ReadyToWatch is not configured.");
                }

                await db
                    .Rounds.Where(round => round.Id == id)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(round => round.Status, ready), ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
    }

    private static async Task<Dictionary<int, string[]>> LoadWinnersAsync(
        MotorsportsDbContext db,
        int[] roundIds,
        CancellationToken cancellationToken
    )
    {
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
            .OrderBy(participant => participant.LastName)
            .ThenBy(participant => participant.FirstName)
            .ToListAsync(cancellationToken);
        return winners
            .GroupBy(winner => winner.Round)
            .ToDictionary(
                group => group.Key,
                group =>
                    group
                        .Select(participant =>
                            CountryFlag.WithName(
                                participant.Country,
                                participant.FirstName + " " + participant.LastName
                            )
                        )
                        .ToArray()
            );
    }

    private static async Task<(string? Venue, string? Status, sbyte StatusStep, Error? Error)> ResolveLookupsAsync(
        MotorsportsDbContext db,
        RoundForm form,
        int? excludeRoundId,
        CancellationToken ct
    )
    {
        var venue = await db
            .Venues.Where(row => row.Name == form.Venue!.Trim())
            .Select(row => row.Name)
            .FirstOrDefaultAsync(ct);
        if (venue is null)
        {
            return (null, null, 0, Error.Validation(nameof(RoundForm.Venue), "Select a valid venue."));
        }

        var statusRow = await db
            .Statuses.Where(row => row.Name == form.Status!.Trim())
            .Select(row => new { row.Name, row.Step })
            .FirstOrDefaultAsync(ct);
        if (statusRow is null)
        {
            return (null, null, 0, Error.Validation(nameof(RoundForm.Status), "Select a valid status."));
        }

        if (
            form.Number != 0
            && await db.Rounds.AnyAsync(
                round =>
                    round.Season == form.SeasonId
                    && round.Number == form.Number
                    && (excludeRoundId == null || round.Id != excludeRoundId),
                ct
            )
        )
        {
            return (
                null,
                null,
                0,
                Error.Validation(nameof(RoundForm.Number), "Round number must be unique in the season.")
            );
        }

        if (
            form.WinningTeam is { } teamId
            && !await db.SeasonEntries.AnyAsync(entry => entry.Season == form.SeasonId && entry.Team == teamId, ct)
        )
        {
            return (
                null,
                null,
                0,
                Error.Validation(nameof(RoundForm.WinningTeam), "Select a team entered in this season.")
            );
        }

        if (
            form.Winners.Count > 0
            && await db.Participants.CountAsync(participant => form.Winners.Contains(participant.Id), ct)
                != form.Winners.Count
        )
        {
            return (null, null, 0, Error.Validation(nameof(RoundForm.Winners), "Select valid participants."));
        }

        return (venue, statusRow.Name, statusRow.Step, null);
    }

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeVersion(
        int season,
        DateOnly date,
        short number,
        string? name,
        string venue,
        string status,
        string? note,
        decimal? rating,
        decimal? rain,
        int? winningTeam,
        IEnumerable<int> winners
    ) =>
        RowVersion.Compute(
            season.ToString(),
            date.ToString("O"),
            number.ToString(),
            name,
            venue,
            status,
            note,
            rating?.ToString(),
            rain?.ToString(),
            winningTeam?.ToString(),
            string.Join(",", winners.Order())
        );
}
