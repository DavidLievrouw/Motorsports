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
            var matchingSports = db
                .Sports.AsNoTracking()
                .Where(s => s.Name == SportName.From(search) || (s.FullName != null && s.FullName.Contains(search)))
                .Select(s => s.Name);
            var matchingNames = query.Where(t =>
                t.Name.Contains(search)
                || matchingSports.Contains(t.Sport)
                || (t.CountryRef!.NiceName != null && t.CountryRef.NiceName.Contains(search))
                || (t.CountryRef.Name != null && t.CountryRef.Name.Contains(search))
            );
            query =
                search.Length == 2
                    ? matchingNames.Union(db.Teams.AsNoTracking().Where(t => t.Country == CountryIso.From(search)))
                    : matchingNames;
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Sport)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * Paging.DefaultPageSize)
            .Take(Paging.DefaultPageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Sport,
                t.Country,
                CountryName = t.CountryRef!.NiceName ?? t.CountryRef.Name,
            })
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(t => new TeamRow(t.Id, t.Name, t.Sport, t.Country, t.CountryName ?? t.Country.Value))
            .ToArray();
        return new PagedResult<TeamRow>(items, total, page, Paging.DefaultPageSize);
    }

    public async Task<TeamInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var projection = await db
            .Teams.AsNoTracking()
            .Where(t => t.Id == TeamId.From(id))
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Sport,
                t.Country,
                CountryName = t.CountryRef!.NiceName ?? t.CountryRef.Name,
                SeasonEntryCount = db.SeasonEntries.Count(e => e.Team == t.Id),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (projection is null)
        {
            return null;
        }

        var team = new TeamInfo(
            projection.Id,
            projection.Name,
            projection.Sport,
            projection.Country,
            projection.CountryName ?? projection.Country.Value,
            projection.SeasonEntryCount,
            Array.Empty<TeamSeasonWin>()
        );
        var seasonWins = await db
            .Seasons.AsNoTracking()
            .Where(season => season.WinningTeam == TeamId.From(id))
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
                return endDateComparison != 0 ? endDateComparison : right.SeasonId.Value.CompareTo(left.SeasonId.Value);
            }
        );
        return team with { SeasonWins = seasonWins };
    }

    public async Task<TeamForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Teams.AsNoTracking()
            .Where(t => t.Id == TeamId.From(id))
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
                Sport = row.Sport.Value,
                Country = row.Country.Value,
                RowVersion = RowVersion.Compute(row.Name, row.Sport.Value, row.Country.Value),
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
                    Sport = lookups.Sport!.Value,
                    Country = lookups.Country!.Value,
                };
                db.Teams.Add(team);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(team.Id.Value);
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
                        RowVersion.Compute(current.Name, current.Sport.Value, current.Country.Value)
                    )
                )
                {
                    return Error.Conflict(StaleMessage);
                }

                if (lookups.Sport != current.Sport)
                {
                    return Error.Validation(nameof(TeamForm.Sport), "The sport of a team cannot be changed.");
                }

                if (
                    await db.Teams.AnyAsync(
                        t => t.Id != TeamId.From(id) && t.Name == name && t.Sport == current.Sport,
                        ct
                    )
                )
                {
                    return Error.Validation(
                        nameof(TeamForm.Name),
                        "A team with this name already exists in this sport."
                    );
                }

                var country = lookups.Country!.Value;
                await db
                    .Teams.Where(t => t.Id == TeamId.From(id))
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

                if (await db.SeasonEntries.AnyAsync(e => e.Team == TeamId.From(id), ct))
                {
                    return Error.Conflict(InUseMessage);
                }

                await db.Teams.Where(t => t.Id == TeamId.From(id)).ExecuteDeleteAsync(ct);
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

    private static async Task<(SportName? Sport, CountryIso? Country, Error? Failure)> ResolveAsync(
        MotorsportsDbContext db,
        string sport,
        string country,
        CancellationToken ct
    )
    {
        var sportKey = sport.Trim();
        var countryKey = country.Trim();
        var canonicalSport = await db
            .Sports.Where(s => s.Name == SportName.From(sportKey))
            .Select(s => (SportName?)s.Name)
            .FirstOrDefaultAsync(ct);
        if (canonicalSport is null)
        {
            return (null, null, Error.Validation(nameof(TeamForm.Sport), "Select a valid sport."));
        }

        var canonicalCountry = await db
            .Countries.Where(c => c.Iso == CountryIso.From(countryKey))
            .Select(c => (CountryIso?)c.Iso)
            .FirstOrDefaultAsync(ct);
        return canonicalCountry is null
            ? (null, null, Error.Validation(nameof(TeamForm.Country), "Select a valid country."))
            : (canonicalSport, canonicalCountry, null);
    }
}
