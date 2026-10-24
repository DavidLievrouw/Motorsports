using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Venues;

public sealed class VenueService(IDbContextFactory<MotorsportsDbContext> factory, IValidator<VenueForm> validator)
{
    public const string StaleMessage =
        "This venue was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public async Task<PagedResult<VenueRow>> ListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default
    )
    {
        search = Paging.NormalizeSearch(search);
        page = Paging.ClampPage(page);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Venues.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(v =>
                v.Name.Contains(search) || (v.CountryRef!.NiceName ?? v.CountryRef.Name ?? v.Country).Contains(search)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * Paging.DefaultPageSize)
            .Take(Paging.DefaultPageSize)
            .Select(v => new VenueRow(v.Name, v.Country, v.CountryRef!.NiceName ?? v.CountryRef.Name ?? v.Country))
            .ToListAsync(cancellationToken);
        return new PagedResult<VenueRow>(items, total, page, Paging.DefaultPageSize);
    }

    public async Task<VenueInfo?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var venue = await db
            .Venues.AsNoTracking()
            .Where(v => v.Name == name)
            .Select(v => new
            {
                v.Name,
                v.Country,
                CountryName = v.CountryRef!.NiceName ?? v.CountryRef.Name ?? v.Country,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (venue is null)
        {
            return null;
        }

        var roundRows = await db
            .Rounds.AsNoTracking()
            .Where(round => round.Venue == venue.Name)
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.Number)
            .Select(round => new
            {
                round.Id,
                round.Date,
                round.Number,
                round.Name,
                round.SeasonRef!.Sport,
                HasImage = round.SeasonRef.SportRef!.Image != null,
                round.Status,
                round.Rating,
                round.Rain,
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
        var rounds = roundRows
            .Select(round => new VenueRoundInfo(
                round.Id,
                round.Date,
                round.Number,
                round.Name,
                RoundLabel.Format(round.Number, round.Name),
                round.Sport,
                round.HasImage,
                round.Status,
                round.Rating,
                RainLevelDisplay.Format(round.Rain),
                round.TeamName is null ? null : CountryFlag.WithName(round.TeamCountry, round.TeamName),
                Array.Empty<string>()
            ))
            .ToList();
        if (rounds.Count == 0)
        {
            return new VenueInfo(venue.Name, venue.Country, venue.CountryName, 0, rounds);
        }

        var roundIds = rounds.Select(round => round.Id).ToArray();
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
            .ToListAsync(cancellationToken);
        var winnersByRound = winners
            .GroupBy(winner => winner.Round)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<string>)
                        group.Select(w => CountryFlag.WithName(w.Country, $"{w.FirstName} {w.LastName}")).ToArray()
            );
        var roundsWithWinners = rounds
            .Select(round =>
                round with
                {
                    WinningParticipants = winnersByRound.GetValueOrDefault(round.Id, Array.Empty<string>()),
                }
            )
            .ToArray();

        return new VenueInfo(venue.Name, venue.Country, venue.CountryName, roundsWithWinners.Length, roundsWithWinners);
    }

    public async Task<VenueForm?> GetFormAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Venues.AsNoTracking()
            .Where(v => v.Name == name)
            .Select(v => new { v.Name, v.Country })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new VenueForm
            {
                Name = row.Name,
                Country = row.Country,
                RowVersion = RowVersion.Compute(row.Name, row.Country),
            };
    }

    public async Task<Result<string>> CreateAsync(VenueForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = form.Name!.Trim();
        return await factory.InTransactionAsync<string>(
            async (db, ct) =>
            {
                var country = await CanonicalCountryAsync(db, form.Country!, ct);
                if (country is null)
                {
                    return Error.Validation(nameof(VenueForm.Country), "Select a valid country.");
                }

                if (await db.Venues.AnyAsync(v => v.Name == name, ct))
                {
                    return Error.Validation(nameof(VenueForm.Name), "A venue with this name already exists.");
                }

                db.Venues.Add(new Venue { Name = name, Country = country });
                await db.SaveChangesAsync(ct);
                return Result<string>.Ok(name);
            },
            cancellationToken: cancellationToken
        );
    }

    public async Task<Result<string>> UpdateAsync(
        string originalName,
        VenueForm form,
        CancellationToken cancellationToken = default
    )
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var newName = form.Name!.Trim();
        return await factory.InTransactionAsync<string>(
            async (db, ct) =>
            {
                var country = await CanonicalCountryAsync(db, form.Country!, ct);
                if (country is null)
                {
                    return Error.Validation(nameof(VenueForm.Country), "Select a valid country.");
                }

                // Lock the row, then compare its current values with the ones the form was loaded from.
                var rows = await db
                    .Venues.FromSql($"SELECT `Name`, `Country` FROM `Venue` WHERE `Name` = {originalName} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Venue not found.");
                }

                var current = rows[0];
                if (!RowVersion.Matches(form.RowVersion, RowVersion.Compute(current.Name, current.Country)))
                {
                    return Error.Conflict(StaleMessage);
                }

                if (string.Equals(newName, current.Name, StringComparison.Ordinal))
                {
                    if (!string.Equals(country, current.Country, StringComparison.Ordinal))
                    {
                        await db
                            .Venues.Where(v => v.Name == current.Name)
                            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Country, country), ct);
                    }

                    return Result<string>.Ok(current.Name);
                }

                // At most one row matches under the case/accent-insensitive collation: this venue (case/accent-only
                // rename) or a different venue (duplicate).
                var match = await db.Venues.Where(v => v.Name == newName).Select(v => v.Name).FirstOrDefaultAsync(ct);
                var sameRow = match is not null && string.Equals(match, current.Name, StringComparison.Ordinal);
                if (match is not null && !sameRow)
                {
                    return Error.Validation(nameof(VenueForm.Name), "A venue with this name already exists.");
                }

                await RenameAsync(db, current.Name, newName, country, sameRow, ct);
                return Result<string>.Ok(newName);
            },
            cancellationToken: cancellationToken
        );
    }

    private static async Task<string?> CanonicalCountryAsync(
        MotorsportsDbContext db,
        string country,
        CancellationToken ct
    )
    {
        var key = country.Trim();
        return await db.Countries.Where(c => c.Iso == key).Select(c => c.Iso).FirstOrDefaultAsync(ct);
    }

    // Name is the primary key, referenced by Round with RESTRICT: insert the new row, repoint, delete the old one.
    // A case-only rename cannot insert the new key next to the old one, so it goes through a temporary key.
    private static async Task RenameAsync(
        MotorsportsDbContext db,
        string oldName,
        string newName,
        string country,
        bool viaTemporaryKey,
        CancellationToken ct
    )
    {
        var interim = viaTemporaryKey ? $"~{Guid.NewGuid():N}" : newName;
        db.Venues.Add(new Venue { Name = interim, Country = country });
        await db.SaveChangesAsync(ct);
        await db
            .Rounds.Where(r => r.Venue == oldName)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Venue, interim), ct);
        await db.Venues.Where(v => v.Name == oldName).ExecuteDeleteAsync(ct);

        if (viaTemporaryKey)
        {
            db.Venues.Add(new Venue { Name = newName, Country = country });
            await db.SaveChangesAsync(ct);
            await db
                .Rounds.Where(r => r.Venue == interim)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Venue, newName), ct);
            await db.Venues.Where(v => v.Name == interim).ExecuteDeleteAsync(ct);
        }
    }
}
