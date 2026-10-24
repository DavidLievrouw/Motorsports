using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Common.Images;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Infrastructure.Lookups;

namespace Motorsports.Sports;

public sealed class SportService(
    IDbContextFactory<MotorsportsDbContext> factory,
    LookupCache cache,
    IValidator<SportForm> validator
)
{
    public const string StaleMessage =
        "This sport was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public async Task<IReadOnlyList<SportRow>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db
            .Sports.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SportRow(s.Name, s.FullName, s.Image != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<SportInfo?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var sport = await db
            .Sports.AsNoTracking()
            .Where(s => s.Name == name)
            .Select(s => new SportInfo(
                s.Name,
                s.FullName,
                db.Teams.Count(t => t.Sport == s.Name),
                db.Seasons.Count(x => x.Sport == s.Name),
                s.Image != null,
                null,
                null,
                Array.Empty<string>(),
                null
            ))
            .FirstOrDefaultAsync(cancellationToken);
        if (sport is null)
        {
            return null;
        }

        var latestSeason = await db
            .Seasons.AsNoTracking()
            .Where(s =>
                s.Sport == name
                && db.Rounds.Any(round => round.Season == s.Id)
                && !db.Rounds.Any(round => round.Season == s.Id && round.StatusRef!.Step < 2)
            )
            .Select(s => new
            {
                s.Id,
                s.Label,
                s.WinningTeam,
                LastDate = db.Rounds.Where(r => r.Season == s.Id).Select(r => (DateOnly?)r.Date).Max(),
                StartDate = db.Rounds.Where(r => r.Season == s.Id).Select(r => (DateOnly?)r.Date).Min(),
            })
            .OrderBy(s => s.StartDate.HasValue ? 1 : 0)
            .ThenByDescending(s => s.StartDate)
            .ThenByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        string? winningTeamName = null;
        string[] winningParticipants = [];
        if (latestSeason is not null)
        {
            if (latestSeason.WinningTeam is { } teamId)
            {
                var teamCountry = await db
                    .Teams.AsNoTracking()
                    .Where(team => team.Id == teamId)
                    .Select(team => team.Country)
                    .FirstOrDefaultAsync(cancellationToken);
                var plainName =
                    await db
                        .SeasonEntries.AsNoTracking()
                        .Where(entry => entry.Season == latestSeason.Id && entry.Team == teamId)
                        .Select(entry => entry.Name)
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? await db
                        .Teams.AsNoTracking()
                        .Where(team => team.Id == teamId)
                        .Select(team => team.Name)
                        .FirstOrDefaultAsync(cancellationToken);
                winningTeamName = plainName is null ? null : CountryFlag.WithName(teamCountry, plainName);
            }

            winningParticipants = await db
                .SeasonWinners.AsNoTracking()
                .Where(winner => winner.Season == latestSeason.Id)
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
                .ToListAsync(cancellationToken)
                .ContinueWith(
                    task =>
                        task.Result.Select(p => CountryFlag.WithName(p.Country, p.FirstName + " " + p.LastName))
                            .ToArray(),
                    cancellationToken
                );
        }

        var nextRoundDate = await db
            .Rounds.AsNoTracking()
            .Where(round => round.SeasonRef!.Sport == name && round.StatusRef!.Step < 2)
            .OrderBy(round => round.Date)
            .Select(round => (DateOnly?)round.Date)
            .FirstOrDefaultAsync(cancellationToken);

        return sport with
        {
            LatestSeasonLabel =
                latestSeason is null ? null
                : !string.IsNullOrWhiteSpace(latestSeason.Label) ? latestSeason.Label
                : latestSeason.LastDate?.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ?? $"#{latestSeason.Id}",
            WinningTeamName = winningTeamName,
            WinningParticipants = winningParticipants,
            NextRoundDate = nextRoundDate,
        };
    }

    public async Task<SportForm?> GetFormAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Sports.AsNoTracking()
            .Where(s => s.Name == name)
            .Select(s => new
            {
                s.Name,
                s.FullName,
                s.Image,
            })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new SportForm
            {
                Name = row.Name,
                FullName = row.FullName,
                Image = row.Image,
                StoredImageName = row.Image is null ? null : row.Name,
                RowVersion = ComputeVersion(row.Name, row.FullName, row.Image),
            };
    }

    public async Task<Result<string>> CreateAsync(SportForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var name = form.Name!.Trim();
        var fullName = Normalize(form.FullName);
        var result = await factory.InTransactionAsync<string>(
            async (db, ct) =>
            {
                if (await db.Sports.AnyAsync(s => s.Name == name, ct))
                {
                    return Error.Validation(nameof(SportForm.Name), "A sport with this name already exists.");
                }

                db.Sports.Add(
                    new Sport
                    {
                        Name = name,
                        FullName = fullName,
                        Image = form.Image,
                    }
                );
                await db.SaveChangesAsync(ct);
                return Result<string>.Ok(name);
            },
            cancellationToken: cancellationToken
        );
        return Published(result);
    }

    public async Task<Result<string>> UpdateAsync(
        string originalName,
        SportForm form,
        CancellationToken cancellationToken = default
    )
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var newName = form.Name!.Trim();
        var fullName = Normalize(form.FullName);
        var result = await factory.InTransactionAsync<string>(
            async (db, ct) =>
            {
                // Lock the row, then compare its current values with the ones the form was loaded from.
                var rows = await db
                    .Sports.FromSql(
                        $"SELECT `Name`, `FullName`, `Image` FROM `Sport` WHERE `Name` = {originalName} FOR UPDATE"
                    )
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Sport not found.");
                }

                var current = rows[0];
                if (!RowVersion.Matches(form.RowVersion, ComputeVersion(current.Name, current.FullName, current.Image)))
                {
                    return Error.Conflict(StaleMessage);
                }

                if (string.Equals(newName, current.Name, StringComparison.Ordinal))
                {
                    if (
                        !string.Equals(fullName, current.FullName, StringComparison.Ordinal)
                        || !ImagesEqual(form.Image, current.Image)
                    )
                    {
                        await db
                            .Sports.Where(s => s.Name == current.Name)
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(x => x.FullName, fullName).SetProperty(x => x.Image, form.Image),
                                ct
                            );
                    }

                    return Result<string>.Ok(current.Name);
                }

                // At most one row matches under the case/accent-insensitive collation: the current row itself
                // (a case/accent-only rename) or a different sport (duplicate).
                var match = await db.Sports.Where(s => s.Name == newName).Select(s => s.Name).FirstOrDefaultAsync(ct);
                var sameRow = match is not null && string.Equals(match, current.Name, StringComparison.Ordinal);
                if (match is not null && !sameRow)
                {
                    return Error.Validation(nameof(SportForm.Name), "A sport with this name already exists.");
                }

                await RenameAsync(db, current.Name, newName, fullName, form.Image, sameRow, ct);
                return Result<string>.Ok(newName);
            },
            cancellationToken: cancellationToken
        );
        return Published(result);
    }

    // Name is the primary key, referenced by Team and Season with RESTRICT: insert the new row, repoint, delete the old one.
    // A case-only rename cannot insert the new key next to the old one, so it goes through a temporary key.
    private static async Task RenameAsync(
        MotorsportsDbContext db,
        string oldName,
        string newName,
        string? fullName,
        byte[]? image,
        bool viaTemporaryKey,
        CancellationToken ct
    )
    {
        var interim = viaTemporaryKey ? $"~{Guid.NewGuid():N}" : newName;
        db.Sports.Add(
            new Sport
            {
                Name = interim,
                FullName = fullName,
                Image = image,
            }
        );
        await db.SaveChangesAsync(ct);
        await Repoint(db, oldName, interim, ct);
        await db.Sports.Where(s => s.Name == oldName).ExecuteDeleteAsync(ct);

        if (viaTemporaryKey)
        {
            db.Sports.Add(
                new Sport
                {
                    Name = newName,
                    FullName = fullName,
                    Image = image,
                }
            );
            await db.SaveChangesAsync(ct);
            await Repoint(db, interim, newName, ct);
            await db.Sports.Where(s => s.Name == interim).ExecuteDeleteAsync(ct);
        }
    }

    private static async Task Repoint(MotorsportsDbContext db, string from, string to, CancellationToken ct)
    {
        await db.Teams.Where(t => t.Sport == from).ExecuteUpdateAsync(s => s.SetProperty(t => t.Sport, to), ct);
        await db.Seasons.Where(x => x.Sport == from).ExecuteUpdateAsync(s => s.SetProperty(x => x.Sport, to), ct);
    }

    private Result<string> Published(Result<string> result)
    {
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Sports);
            cache.Invalidate(LookupKind.Teams);
            cache.Invalidate(LookupKind.SeasonEntries);
        }

        return result;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeVersion(string name, string? fullName, byte[]? image) =>
        RowVersion.Compute(
            name,
            fullName,
            image is null ? null : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image))
        );

    private static bool ImagesEqual(byte[]? left, byte[]? right) =>
        left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);
}
