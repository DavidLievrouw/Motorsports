using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Infrastructure.Lookups;

namespace Motorsports.Seasons;

internal sealed class SeasonService(
    IDbContextFactory<MotorsportsDbContext> factory,
    LookupCache cache,
    IValidator<SeasonForm> validator
)
{
    public const string StaleMessage =
        "This season was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public const string HasRoundsMessage = "A season can only be deleted when it has no rounds.";

    public async Task<IReadOnlyList<SeasonRow>> ListAsync(
        string? sport = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Seasons.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(sport))
        {
            var sportName = SportName.From(sport);
            query = query.Where(season => season.Sport == sportName);
        }

        var rows = query.Select(season => new
        {
            season.Id,
            season.Sport,
            SportFullName = season.SportRef!.FullName,
            SportHasImage = season.SportRef.Image != null,
            season.Label,
            StartDate = db
                .Rounds.Where(round => round.Season == season.Id)
                .Select(round => (DateOnly?)round.Date)
                .Min(),
            EndDate = db.Rounds.Where(round => round.Season == season.Id).Select(round => (DateOnly?)round.Date).Max(),
            EntryCount = db.SeasonEntries.Count(entry => entry.Season == season.Id),
            RoundCount = db.Rounds.Count(round => round.Season == season.Id),
        });
        return await rows.OrderBy(season => season.Sport)
            .ThenBy(season => season.StartDate.HasValue ? 1 : 0)
            .ThenByDescending(season => season.StartDate)
            .ThenByDescending(season => season.Id)
            .Select(season => new SeasonRow(
                season.Id,
                season.Sport,
                season.SportFullName,
                season.SportHasImage,
                season.Label,
                season.StartDate,
                season.EndDate,
                season.EntryCount,
                season.RoundCount
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<SeasonInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var seasonId = SeasonId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var season = await db
            .Seasons.AsNoTracking()
            .Where(row => row.Id == seasonId)
            .Select(row => new
            {
                row.Id,
                row.Sport,
                SportFullName = row.SportRef!.FullName,
                row.Label,
                row.WinningTeam,
                StartDate = db
                    .Rounds.Where(round => round.Season == row.Id)
                    .Select(round => (DateOnly?)round.Date)
                    .Min(),
                EndDate = db.Rounds.Where(round => round.Season == row.Id).Select(round => (DateOnly?)round.Date).Max(),
                SeasonEntryCount = db.SeasonEntries.Count(entry => entry.Season == row.Id),
                RoundCount = db.Rounds.Count(round => round.Season == row.Id),
                WinningTeamDetails = row.WinningTeam == null
                    ? null
                    : db
                        .SeasonEntries.Where(entry => entry.Season == row.Id && entry.Team == row.WinningTeam)
                        .Select(entry => new { entry.Name, entry.TeamRef!.Country })
                        .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (season is null)
        {
            return null;
        }

        var winners = await db
            .SeasonWinners.AsNoTracking()
            .Where(winner => winner.Season == seasonId)
            .Join(
                db.Participants.AsNoTracking(),
                winner => winner.Participant,
                participant => participant.Id,
                (_, participant) =>
                    new
                    {
                        participant.FirstName,
                        participant.LastName,
                        participant.Country,
                    }
            )
            .OrderBy(participant => participant.LastName)
            .ThenBy(participant => participant.FirstName)
            .ToArrayAsync(cancellationToken);
        var winnerNames = winners
            .Select(participant =>
                CountryFlag.WithName(participant.Country.Value, participant.FirstName + " " + participant.LastName)
            )
            .ToArray();

        return new SeasonInfo(
            season.Id,
            season.Sport,
            season.SportFullName,
            season.Label,
            season.StartDate,
            season.EndDate,
            season.WinningTeam,
            season.WinningTeamDetails is { } winningTeam
                ? CountryFlag.WithName(winningTeam.Country.Value, winningTeam.Name)
                : null,
            winnerNames,
            season.SeasonEntryCount,
            season.RoundCount
        );
    }

    public async Task<SeasonForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var seasonId = SeasonId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Seasons.AsNoTracking()
            .Where(season => season.Id == seasonId)
            .Select(season => new
            {
                season.Sport,
                season.Label,
                season.WinningTeam,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var winners = await db
            .SeasonWinners.AsNoTracking()
            .Where(winner => winner.Season == seasonId)
            .OrderBy(winner => winner.Participant)
            .Select(winner => winner.Participant)
            .ToListAsync(cancellationToken);
        return new SeasonForm
        {
            Sport = row.Sport.Value,
            Label = row.Label,
            WinningTeam = row.WinningTeam?.Value,
            Winners = winners.Select(winner => winner.Value).ToList(),
            RowVersion = ComputeVersion(row.Sport, row.Label, row.WinningTeam, winners),
        };
    }

    public async Task<Result<int>> CreateAsync(SeasonForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var label = form.Label!.Trim();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var sport = await db
                    .Sports.Where(row => row.Name == SportName.From(form.Sport!.Trim()))
                    .Select(row => (SportName?)row.Name)
                    .FirstOrDefaultAsync(ct);
                if (sport is null)
                {
                    return Error.Validation(nameof(SeasonForm.Sport), "Select a valid sport.");
                }

                var season = new Season { Sport = sport.Value, Label = label };
                db.Seasons.Add(season);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(season.Id.Value);
            },
            cancellationToken: cancellationToken
        );
        InvalidateAfterWrite(result);
        return result;
    }

    public async Task<Result<int>> UpdateAsync(int id, SeasonForm form, CancellationToken cancellationToken = default)
    {
        var seasonId = SeasonId.From(id);
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        if (form.Winners is null || form.Winners.Count != form.Winners.Distinct().Count())
        {
            return Error.Validation(nameof(SeasonForm.Winners), "Select each participant only once.");
        }

        var participantIds = form.Winners.Select(ParticipantId.From).ToArray();
        TeamId? winningTeamId = form.WinningTeam is { } formWinningTeam ? TeamId.From(formWinningTeam) : null;
        var result = await factory.InTransactionAsync(
            (db, ct) => UpdateInTransactionAsync(db, seasonId, form, participantIds, winningTeamId, ct),
            IsolationLevel.Serializable,
            cancellationToken
        );
        InvalidateAfterWrite(result);
        return result;
    }

    private static async Task<Result<int>> UpdateInTransactionAsync(
        MotorsportsDbContext db,
        SeasonId seasonId,
        SeasonForm form,
        ParticipantId[] participantIds,
        TeamId? winningTeamId,
        CancellationToken ct
    )
    {
        var rows = await db
            .Seasons.FromSql($"SELECT * FROM `Season` WHERE `Id` = {seasonId.Value} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            return Error.NotFound("Season not found.");
        }

        var current = rows[0];
        var winnerIds = await db
            .SeasonWinners.Where(winner => winner.Season == seasonId)
            .OrderBy(winner => winner.Participant)
            .Select(winner => winner.Participant)
            .ToListAsync(ct);
        if (
            !RowVersion.Matches(
                form.RowVersion,
                ComputeVersion(current.Sport, current.Label, current.WinningTeam, winnerIds)
            )
        )
        {
            return Error.Conflict(StaleMessage);
        }

        var validation = await ValidateUpdateReferencesAsync(db, current, form, participantIds, winningTeamId, ct);
        if (!validation.IsSuccess)
        {
            return validation.Error;
        }

        var label = form.Label!.Trim();
        await db
            .Seasons.Where(season => season.Id == seasonId)
            .ExecuteUpdateAsync(
                updates =>
                    updates
                        .SetProperty(season => season.Label, label)
                        .SetProperty(season => season.WinningTeam, winningTeamId),
                ct
            );
        await db.SeasonWinners.Where(winner => winner.Season == seasonId).ExecuteDeleteAsync(ct);
        if (participantIds.Length > 0)
        {
            db.SeasonWinners.AddRange(
                participantIds.Select(participant => new SeasonWinner { Season = seasonId, Participant = participant })
            );
            await db.SaveChangesAsync(ct);
        }

        return Result<int>.Ok(seasonId.Value);
    }

    private static async Task<Result<Unit>> ValidateUpdateReferencesAsync(
        MotorsportsDbContext db,
        Season current,
        SeasonForm form,
        ParticipantId[] participantIds,
        TeamId? winningTeamId,
        CancellationToken ct
    )
    {
        var sport = await db
            .Sports.Where(row => row.Name == SportName.From(form.Sport!.Trim()))
            .Select(row => (SportName?)row.Name)
            .FirstOrDefaultAsync(ct);
        if (sport is null)
        {
            return Error.Validation(nameof(SeasonForm.Sport), "Select a valid sport.");
        }

        if (sport.Value != current.Sport)
        {
            return Error.Validation(nameof(SeasonForm.Sport), "The sport of a season cannot be changed.");
        }

        if (
            winningTeamId is { } teamId
            && !await db.SeasonEntries.AnyAsync(entry => entry.Season == current.Id && entry.Team == teamId, ct)
        )
        {
            return Error.Validation(nameof(SeasonForm.WinningTeam), "Select a team entered in this season.");
        }

        if (
            participantIds.Length > 0
            && await db.Participants.CountAsync(participant => participantIds.Contains(participant.Id), ct)
                != participantIds.Length
        )
        {
            return Error.Validation(nameof(SeasonForm.Winners), "Select valid participants.");
        }

        return Result.Ok();
    }

    public async Task<SeasonDeleteInfo?> GetDeleteInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var seasonId = SeasonId.From(id);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .Seasons.AsNoTracking()
            .Where(season => season.Id == seasonId)
            .Select(season => new SeasonDeleteInfo(
                season.Id,
                season.Label,
                db.Rounds.Count(round => round.Season == seasonId),
                db.SeasonEntries.Count(entry => entry.Season == seasonId),
                db.SeasonWinners.Count(winner => winner.Season == seasonId)
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Unit>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var seasonId = SeasonId.From(id);
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Seasons.FromSql($"SELECT * FROM `Season` WHERE `Id` = {seasonId.Value} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Season not found.");
                }

                if (await db.Rounds.AnyAsync(round => round.Season == seasonId, ct))
                {
                    return Error.Conflict(HasRoundsMessage);
                }

                await db.SeasonWinners.Where(winner => winner.Season == seasonId).ExecuteDeleteAsync(ct);
                await db.SeasonEntries.Where(entry => entry.Season == seasonId).ExecuteDeleteAsync(ct);
                await db.Seasons.Where(season => season.Id == seasonId).ExecuteDeleteAsync(ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        InvalidateAfterWrite(result);
        return result;
    }

    private void InvalidateAfterWrite<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Teams);
            cache.Invalidate(LookupKind.SeasonEntries);
        }
    }

    private static string ComputeVersion(
        SportName sport,
        string? label,
        TeamId? winningTeam,
        IEnumerable<ParticipantId> winners
    ) =>
        RowVersion.Compute(
            sport.Value,
            label,
            winningTeam?.Value.ToString(),
            string.Join(",", winners.Select(winner => winner.Value).Order())
        );
}
