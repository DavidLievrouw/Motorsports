using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Infrastructure.Lookups;

namespace Motorsports.Seasons;

internal sealed class SeasonEntryService(
    IDbContextFactory<MotorsportsDbContext> factory,
    LookupCache cache,
    IValidator<SeasonEntryForm> validator
)
{
    public const string StaleMessage =
        "This season entry was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public const string WinningEntryMessage = "A season entry that has won a season or round cannot be deleted.";

    public async Task<SeasonEntryContext?> GetSeasonAsync(int seasonId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .Seasons.AsNoTracking()
            .Where(season => season.Id == seasonId)
            .Select(season => new SeasonEntryContext(season.Id, season.Label, season.Sport))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeasonEntryRow>> ListAsync(
        int seasonId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .SeasonEntries.AsNoTracking()
            .Where(entry => entry.Season == seasonId)
            .OrderBy(entry => entry.TeamRef!.Name)
            .ThenBy(entry => entry.Name)
            .Select(entry => new SeasonEntryRow(
                entry.Season,
                entry.Team,
                entry.Name,
                entry.TeamRef!.Name,
                entry.TeamRef.Country,
                entry.TeamRef.CountryRef!.NiceName ?? entry.TeamRef.CountryRef.Name ?? entry.TeamRef.Country
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<SeasonEntryInfo?> GetAsync(
        int seasonId,
        int teamId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .SeasonEntries.AsNoTracking()
            .Where(entry => entry.Season == seasonId && entry.Team == teamId)
            .Select(entry => new SeasonEntryInfo(
                entry.Season,
                entry.Team,
                entry.SeasonRef!.Label,
                entry.SeasonRef.Sport,
                entry.Name,
                entry.TeamRef!.Name,
                entry.TeamRef.Country,
                entry.TeamRef.CountryRef!.NiceName ?? entry.TeamRef.CountryRef.Name ?? entry.TeamRef.Country,
                entry.SeasonRef.WinningTeam == entry.Team,
                db.Rounds.Any(round => round.Season == entry.Season && round.WinningTeam == entry.Team)
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SeasonEntryForm?> GetFormAsync(
        int seasonId,
        int teamId,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var name = await db
            .SeasonEntries.AsNoTracking()
            .Where(entry => entry.Season == seasonId && entry.Team == teamId)
            .Select(entry => entry.Name)
            .FirstOrDefaultAsync(cancellationToken);
        return name is null
            ? null
            : new SeasonEntryForm
            {
                TeamId = teamId,
                Name = name,
                RowVersion = ComputeVersion(seasonId, teamId, name),
            };
    }

    public async Task<Result<int>> CreateAsync(
        int seasonId,
        SeasonEntryForm form,
        CancellationToken cancellationToken = default
    )
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        if (form.TeamId is null)
        {
            return Error.Validation(nameof(SeasonEntryForm.TeamId), "Select a team.");
        }

        var name = form.Name!.Trim();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var season = await db
                    .Seasons.Where(row => row.Id == seasonId)
                    .Select(row => row.Sport)
                    .FirstOrDefaultAsync(ct);
                if (season is null)
                {
                    return Error.NotFound("Season not found.");
                }

                if (!await db.Teams.AnyAsync(row => row.Id == form.TeamId.Value && row.Sport == season, ct))
                {
                    return Error.Validation(nameof(SeasonEntryForm.TeamId), "Select a team from this season's sport.");
                }

                if (await db.SeasonEntries.AnyAsync(entry => entry.Season == seasonId && entry.Team == form.TeamId, ct))
                {
                    return Error.Validation("", "This team already has an entry in the season.");
                }

                db.SeasonEntries.Add(
                    new SeasonEntry
                    {
                        Season = seasonId,
                        Team = form.TeamId.Value,
                        Name = name,
                    }
                );
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(form.TeamId.Value);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        InvalidateAfterWrite(result);
        return result;
    }

    public async Task<Result<int>> UpdateAsync(
        int seasonId,
        int teamId,
        SeasonEntryForm form,
        CancellationToken cancellationToken = default
    )
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        if (form.TeamId != teamId)
        {
            return Error.Validation(nameof(SeasonEntryForm.TeamId), "The team in a season entry cannot be changed.");
        }

        var name = form.Name!.Trim();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .SeasonEntries.FromSql(
                        $"SELECT * FROM `SeasonEntry` WHERE `Season` = {seasonId} AND `Team` = {teamId} FOR UPDATE"
                    )
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Season entry not found.");
                }

                if (!RowVersion.Matches(form.RowVersion, ComputeVersion(seasonId, teamId, rows[0].Name)))
                {
                    return Error.Conflict(StaleMessage);
                }

                await db
                    .SeasonEntries.Where(entry => entry.Season == seasonId && entry.Team == teamId)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(entry => entry.Name, name), ct);
                return Result<int>.Ok(teamId);
            },
            cancellationToken: cancellationToken
        );
        InvalidateAfterWrite(result);
        return result;
    }

    public async Task<Result<Unit>> DeleteAsync(int seasonId, int teamId, CancellationToken cancellationToken = default)
    {
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .SeasonEntries.FromSql(
                        $"SELECT * FROM `SeasonEntry` WHERE `Season` = {seasonId} AND `Team` = {teamId} FOR UPDATE"
                    )
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Season entry not found.");
                }

                if (
                    await db.Seasons.AnyAsync(season => season.Id == seasonId && season.WinningTeam == teamId, ct)
                    || await db.Rounds.AnyAsync(round => round.Season == seasonId && round.WinningTeam == teamId, ct)
                )
                {
                    return Error.Conflict(WinningEntryMessage);
                }

                await db
                    .SeasonEntries.Where(entry => entry.Season == seasonId && entry.Team == teamId)
                    .ExecuteDeleteAsync(ct);
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

    private static string ComputeVersion(int seasonId, int teamId, string name) =>
        RowVersion.Compute(seasonId.ToString(), teamId.ToString(), name);
}
