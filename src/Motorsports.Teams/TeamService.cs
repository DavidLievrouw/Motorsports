using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Infrastructure.Lookups;

namespace Motorsports.Teams;

internal sealed class TeamService(
    IDbContextFactory<MotorsportsDbContext> factory,
    IValidator<TeamForm> validator,
    LookupCache cache
)
{
    public const string StaleMessage =
        "This team was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public const string InUseMessage = "This team has season entries and cannot be deleted.";

    public async Task<PagedResult<TeamRow>> ListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default
    )
    {
        search = Paging.NormalizeSearch(search);
        page = Paging.ClampPage(page);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Teams.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(t =>
                t.Name.Contains(search)
                || t.Sport.Contains(search)
                || (t.CountryRef!.NiceName ?? t.CountryRef.Name ?? t.Country).Contains(search)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Sport)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * Paging.DefaultPageSize)
            .Take(Paging.DefaultPageSize)
            .Select(t => new TeamRow(
                t.Id,
                t.Name,
                t.Sport,
                t.Country,
                t.CountryRef!.NiceName ?? t.CountryRef.Name ?? t.Country
            ))
            .ToListAsync(cancellationToken);
        return new PagedResult<TeamRow>(items, total, page, Paging.DefaultPageSize);
    }

    public async Task<TeamInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var team = await db
            .Teams.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TeamInfo(
                t.Id,
                t.Name,
                t.Sport,
                t.Country,
                t.CountryRef!.NiceName ?? t.CountryRef.Name ?? t.Country,
                db.SeasonEntries.Count(e => e.Team == t.Id),
                Array.Empty<TeamSeasonWin>()
            ))
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return null;
        }

        var seasonWins = await db
            .Seasons.AsNoTracking()
            .Where(season => season.WinningTeam == id)
            .Select(season => new TeamSeasonWin(
                season.Id,
                season.Sport,
                season.Label,
                db.Rounds.Where(round => round.Season == season.Id).Select(round => (DateOnly?)round.Date).Min(),
                db.Rounds.Where(round => round.Season == season.Id).Select(round => (DateOnly?)round.Date).Max()
            ))
            .ToArrayAsync(cancellationToken);
        Array.Sort(
            seasonWins,
            static (left, right) =>
            {
                var endDateComparison = Comparer<DateOnly?>.Default.Compare(right.EndDate, left.EndDate);
                return endDateComparison != 0 ? endDateComparison : right.SeasonId.CompareTo(left.SeasonId);
            }
        );
        return team with { SeasonWins = seasonWins };
    }

    public async Task<TeamForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Teams.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Name,
                t.Sport,
                t.Country,
            })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new TeamForm
            {
                Name = row.Name,
                Sport = row.Sport,
                Country = row.Country,
                RowVersion = RowVersion.Compute(row.Name, row.Sport, row.Country),
            };
    }

    public async Task<Result<int>> CreateAsync(TeamForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = form.Name!.Trim();
        // No unique index backs (Name, Sport), so serializable isolation guards the duplicate check against races.
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var lookups = await ResolveAsync(db, form.Sport!, form.Country!, ct);
                if (lookups.Failure is { } failure)
                {
                    return failure;
                }

                if (await db.Teams.AnyAsync(t => t.Name == name && t.Sport == lookups.Sport, ct))
                {
                    return Error.Validation(
                        nameof(TeamForm.Name),
                        "A team with this name already exists in this sport."
                    );
                }

                var team = new Team
                {
                    Name = name,
                    Sport = lookups.Sport!,
                    Country = lookups.Country!,
                };
                db.Teams.Add(team);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(team.Id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Teams);
            cache.Invalidate(LookupKind.SeasonEntries);
        }

        return result;
    }

    public async Task<Result<int>> UpdateAsync(int id, TeamForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = form.Name!.Trim();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var lookups = await ResolveAsync(db, form.Sport!, form.Country!, ct);
                if (lookups.Failure is { } failure)
                {
                    return failure;
                }

                var rows = await db
                    .Teams.FromSql($"SELECT * FROM `Team` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Team not found.");
                }

                var current = rows[0];
                if (
                    !RowVersion.Matches(
                        form.RowVersion,
                        RowVersion.Compute(current.Name, current.Sport, current.Country)
                    )
                )
                {
                    return Error.Conflict(StaleMessage);
                }

                if (!string.Equals(lookups.Sport, current.Sport, StringComparison.Ordinal))
                {
                    return Error.Validation(nameof(TeamForm.Sport), "The sport of a team cannot be changed.");
                }

                if (await db.Teams.AnyAsync(t => t.Id != id && t.Name == name && t.Sport == current.Sport, ct))
                {
                    return Error.Validation(
                        nameof(TeamForm.Name),
                        "A team with this name already exists in this sport."
                    );
                }

                var country = lookups.Country!;
                await db
                    .Teams.Where(t => t.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Name, name).SetProperty(t => t.Country, country), ct);
                return Result<int>.Ok(id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Teams);
            cache.Invalidate(LookupKind.SeasonEntries);
        }

        return result;
    }

    public async Task<Result<Unit>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var rows = await db
                    .Teams.FromSql($"SELECT * FROM `Team` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Team not found.");
                }

                if (await db.SeasonEntries.AnyAsync(e => e.Team == id, ct))
                {
                    return Error.Conflict(InUseMessage);
                }

                await db.Teams.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
                return Result.Ok();
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Teams);
            cache.Invalidate(LookupKind.SeasonEntries);
        }

        return result;
    }

    private static async Task<(string? Sport, string? Country, Error? Failure)> ResolveAsync(
        MotorsportsDbContext db,
        string sport,
        string country,
        CancellationToken ct
    )
    {
        var sportKey = sport.Trim();
        var countryKey = country.Trim();
        var canonicalSport = await db.Sports.Where(s => s.Name == sportKey).Select(s => s.Name).FirstOrDefaultAsync(ct);
        if (canonicalSport is null)
        {
            return (null, null, Error.Validation(nameof(TeamForm.Sport), "Select a valid sport."));
        }

        var canonicalCountry = await db
            .Countries.Where(c => c.Iso == countryKey)
            .Select(c => c.Iso)
            .FirstOrDefaultAsync(ct);
        return canonicalCountry is null
            ? (null, null, Error.Validation(nameof(TeamForm.Country), "Select a valid country."))
            : (canonicalSport, canonicalCountry, null);
    }
}
