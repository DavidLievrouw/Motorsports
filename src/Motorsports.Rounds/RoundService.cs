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
        var id = SeasonId.From(seasonId);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .Seasons.AsNoTracking()
            .Where(season => season.Id == id)
            .Select(season => new RoundSeasonContext(season.Id, season.Sport, season.Label))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoundRow>> ListAsync(int seasonId, CancellationToken cancellationToken = default)
    {
        var id = SeasonId.From(seasonId);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var rows = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Season == id)
            .Join(
                db.Venues.AsNoTracking(),
                round => round.Venue,
                venue => venue.Name,
                (round, venue) => new { Round = round, VenueCountry = venue.Country }
            )
            .OrderBy(row => row.Round.Date)
            .ThenBy(row => row.Round.Number)
            .ThenBy(row => row.Round.Id)
            .Select(row => new
            {
                row.Round.Id,
                row.Round.Date,
                row.Round.Number,
                row.Round.Name,
                row.Round.Venue,
                row.VenueCountry,
                row.Round.Status,
                StatusStep = row.Round.StatusRef!.Step,
                row.Round.Rating,
                row.Round.WinningTeam,
                TeamCountry = row.Round.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
                        .Select(team => (CountryIso?)team.Country)
                        .FirstOrDefault(),
                TeamName = row.Round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry =>
                            entry.Season == row.Round.Season && entry.Team == row.Round.WinningTeam.Value
                        )
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
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
                row.VenueCountry,
                row.Status,
                row.StatusStep,
                row.Rating,
                row.TeamName,
                row.TeamCountry?.Value,
                winners.GetValueOrDefault(row.Id, [])
            )),
        ];
    }

    public async Task<RoundInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var roundId = RoundId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var round = await db
            .Rounds.AsNoTracking()
            .Where(row => row.Id == roundId)
            .Join(
                db.Venues.AsNoTracking(),
                row => row.Venue,
                venue => venue.Name,
                (row, venue) => new { Round = row, VenueCountry = venue.Country }
            )
            .Select(row => new
            {
                row.Round.Id,
                row.Round.Season,
                row.Round.SeasonRef!.Sport,
                SeasonLabel = row.Round.SeasonRef.Label,
                row.Round.Date,
                row.Round.Number,
                row.Round.Name,
                row.Round.Venue,
                row.VenueCountry,
                row.Round.Status,
                StatusStep = row.Round.StatusRef!.Step,
                row.Round.Rating,
                row.Round.Rain,
                row.Round.Note,
                row.Round.WinningTeam,
                TeamCountry = row.Round.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
                        .Select(team => (CountryIso?)team.Country)
                        .FirstOrDefault(),
                TeamName = row.Round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry =>
                            entry.Season == row.Round.Season && entry.Team == row.Round.WinningTeam.Value
                        )
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
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
            round.VenueCountry,
            round.Status,
            round.StatusStep,
            round.Rating,
            round.Rain,
            round.Note,
            round.WinningTeam,
            round.TeamName,
            round.TeamCountry?.Value,
            winners.GetValueOrDefault(round.Id, [])
        );
    }

    public async Task<IReadOnlyList<EventHistoryItem>> GetEventHistoryAsync(
        int roundId,
        CancellationToken cancellationToken = default
    )
    {
        var id = RoundId.From(roundId);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var source = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == id)
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
            .Join(
                db.Venues.AsNoTracking(),
                round => round.Venue,
                venue => venue.Name,
                (round, venue) => new { Round = round, VenueCountry = venue.Country }
            )
            .OrderByDescending(row => row.Round.Date)
            .ThenByDescending(row => row.Round.Id)
            .Take(50)
            .Select(row => new
            {
                row.Round.Id,
                row.Round.Date,
                row.Round.Number,
                row.Round.Name,
                row.Round.Venue,
                row.VenueCountry,
                row.Round.Status,
                row.Round.Rating,
                row.Round.Rain,
                row.Round.WinningTeam,
                TeamCountry = row.Round.WinningTeam == null
                    ? null
                    : db
                        .Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
                        .Select(team => (CountryIso?)team.Country)
                        .FirstOrDefault(),
                TeamName = row.Round.WinningTeam == null
                    ? null
                    : db.SeasonEntries.Where(entry =>
                            entry.Season == row.Round.Season && entry.Team == row.Round.WinningTeam.Value
                        )
                        .Select(entry => entry.Name)
                        .FirstOrDefault()
                        ?? db.Teams.Where(team => team.Id == row.Round.WinningTeam.Value)
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
                row.VenueCountry,
                row.Status,
                row.Rating,
                row.Rain,
                row.TeamName,
                row.TeamCountry?.Value,
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
        var id = SeasonId.From(seasonId);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Seasons.AnyAsync(season => season.Id == id, cancellationToken))
        {
            return null;
        }

        var last = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Season == id)
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.Id)
            .Select(round => new { round.Date, round.Number })
            .FirstOrDefaultAsync(cancellationToken);
        var scheduled = await db
            .Statuses.AsNoTracking()
            .Where(status => status.Name == StatusName.Scheduled)
            .Select(status => (StatusName?)status.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var nextNumber = last is null ? (short)1 : (short)Math.Min(99, last.Number + 1);
        return new RoundForm
        {
            SeasonId = seasonId,
            Date = last?.Date ?? today,
            Number = nextNumber,
            Status = scheduled?.Value ?? StatusName.Scheduled.Value,
        };
    }

    public async Task<RoundForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var roundId = RoundId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == roundId)
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
            .Where(winner => winner.Round == roundId)
            .OrderBy(winner => winner.Participant)
            .Select(winner => winner.Participant)
            .ToListAsync(cancellationToken);
        return new RoundForm
        {
            SeasonId = row.Season.Value,
            Date = row.Date,
            OriginalDate = row.Date,
            Number = row.Number,
            Name = row.Name,
            Venue = row.Venue.Value,
            Status = row.Status.Value,
            Note = row.Note,
            Rating = row.Rating is { } rating ? decimal.ToInt32(rating) : null,
            Rain = row.Rain is { } rain ? (RainLevel)(int)rain : null,
            WinningTeam = row.WinningTeam?.Value,
            Winners = winners.Select(winner => winner.Value).ToList(),
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

        var seasonId = SeasonId.From(form.SeasonId);
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                if (!await db.Seasons.AnyAsync(season => season.Id == seasonId, ct))
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
                    Season = seasonId,
                    Date = form.Date!.Value,
                    Number = form.Number,
                    Name = BlankToNull(form.Name),
                    Venue = lookup.Venue!.Value,
                    Status = lookup.Status!.Value,
                    Note = BlankToNull(form.Note),
                };
                db.Rounds.Add(entity);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(entity.Id.Value);
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

        var roundId = RoundId.From(id);
        var seasonId = SeasonId.From(form.SeasonId);
        var participantIds = form.Winners.Select(ParticipantId.From).ToArray();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {roundId.Value} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Round not found.");
                }

                var current = rows[0];
                if (current.Season != seasonId)
                {
                    return Error.Validation(nameof(RoundForm.SeasonId), "The season of a round cannot be changed.");
                }

                var winnerIds = await db
                    .RoundWinners.Where(winner => winner.Round == roundId)
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

                var lookup = await ResolveLookupsAsync(db, form, roundId, ct);
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
                var venue = lookup.Venue!.Value;
                var status = lookup.Status!.Value;
                TeamId? winningTeam = form.WinningTeam is { } value ? TeamId.From(value) : null;
                var number = form.Number;

                await db
                    .Rounds.Where(round => round.Id == roundId)
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
                await db.RoundWinners.Where(winner => winner.Round == roundId).ExecuteDeleteAsync(ct);
                if (participantIds.Length > 0)
                {
                    db.RoundWinners.AddRange(
                        participantIds.Select(participant => new RoundWinner
                        {
                            Round = roundId,
                            Participant = participant,
                        })
                    );
                    await db.SaveChangesAsync(ct);
                }

                return Result<int>.Ok(roundId.Value);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        return result;
    }

    public async Task<RoundDeleteInfo?> GetDeleteInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var roundId = RoundId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Id == roundId)
            .Select(round => new
            {
                round.Id,
                round.Season,
                round.Number,
                round.Name,
                HasWinners = db.RoundWinners.Any(winner => winner.Round == roundId),
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
        var roundId = RoundId.From(id);
        return await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {roundId.Value} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Round not found.");
                }

                var current = rows[0];
                if (
                    current.WinningTeam is not null
                    || await db.RoundWinners.AnyAsync(winner => winner.Round == roundId, ct)
                )
                {
                    return Error.Conflict(HasWinnersMessage);
                }

                await db.Rounds.Where(round => round.Id == roundId).ExecuteDeleteAsync(ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
    }

    public async Task<Result<Unit>> MarkReadyToWatchAsync(int id, CancellationToken cancellationToken = default)
    {
        var roundId = RoundId.From(id);
        return await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Rounds.FromSql($"SELECT * FROM `Round` WHERE `Id` = {roundId.Value} FOR UPDATE")
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
                    .Statuses.Where(status => status.Name == StatusName.ReadyToWatch)
                    .Select(status => (StatusName?)status.Name)
                    .FirstOrDefaultAsync(ct);
                if (ready is null)
                {
                    return Error.Validation(nameof(RoundForm.Status), "Status ReadyToWatch is not configured.");
                }

                await db
                    .Rounds.Where(round => round.Id == roundId)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(round => round.Status, ready.Value), ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
    }

    private static async Task<Dictionary<RoundId, string[]>> LoadWinnersAsync(
        MotorsportsDbContext db,
        RoundId[] roundIds,
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
                        Country = participant.Country.Value,
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

    private static async Task<(
        VenueName? Venue,
        StatusName? Status,
        sbyte StatusStep,
        Error? Error
    )> ResolveLookupsAsync(MotorsportsDbContext db, RoundForm form, RoundId? excludeRoundId, CancellationToken ct)
    {
        var seasonId = SeasonId.From(form.SeasonId);
        var venue = await db
            .Venues.Where(row => row.Name == VenueName.From(form.Venue!.Trim()))
            .Select(row => (VenueName?)row.Name)
            .FirstOrDefaultAsync(ct);
        if (venue is null)
        {
            return (null, null, 0, Error.Validation(nameof(RoundForm.Venue), "Select a valid venue."));
        }

        var statusRow = await db
            .Statuses.Where(row => row.Name == StatusName.From(form.Status!.Trim()))
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
                    round.Season == seasonId
                    && round.Number == form.Number
                    && (excludeRoundId == null || round.Id != excludeRoundId.Value),
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
            && !await db.SeasonEntries.AnyAsync(
                entry => entry.Season == seasonId && entry.Team == TeamId.From(teamId),
                ct
            )
        )
        {
            return (
                null,
                null,
                0,
                Error.Validation(nameof(RoundForm.WinningTeam), "Select a team entered in this season.")
            );
        }

        var participantIds = form.Winners.Select(ParticipantId.From).ToArray();
        if (
            form.Winners.Count > 0
            && await db.Participants.CountAsync(participant => participantIds.Contains(participant.Id), ct)
                != participantIds.Length
        )
        {
            return (null, null, 0, Error.Validation(nameof(RoundForm.Winners), "Select valid participants."));
        }

        return (venue, statusRow.Name, statusRow.Step, null);
    }

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeVersion(
        SeasonId season,
        DateOnly date,
        short number,
        string? name,
        VenueName venue,
        StatusName status,
        string? note,
        decimal? rating,
        decimal? rain,
        TeamId? winningTeam,
        IEnumerable<ParticipantId> winners
    ) =>
        RowVersion.Compute(
            season.Value.ToString(),
            date.ToString("O"),
            number.ToString(),
            name,
            venue.Value,
            status.Value,
            note,
            rating?.ToString(),
            rain?.ToString(),
            winningTeam?.Value.ToString(),
            string.Join(",", winners.Select(winner => winner.Value).Order())
        );
}
