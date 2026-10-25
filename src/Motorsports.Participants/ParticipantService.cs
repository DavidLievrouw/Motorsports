using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Entities;
using Motorsports.Infrastructure.Lookups;

namespace Motorsports.Participants;

internal sealed class ParticipantService(
    IDbContextFactory<MotorsportsDbContext> factory,
    IValidator<ParticipantForm> validator,
    LookupCache cache
)
{
    public const string StaleMessage =
        "This participant was changed by someone else since you opened it. Your entries are kept; reload to see the current values.";

    public const string DuplicateMessage = "A participant with the same title, name and country already exists.";

    public async Task<PagedResult<ParticipantRow>> ListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default
    )
    {
        search = Paging.NormalizeSearch(search);
        page = Paging.ClampPage(page);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Participants.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(p =>
                p.FirstName.Contains(search)
                || p.LastName.Contains(search)
                || (p.CountryRef!.NiceName ?? p.CountryRef.Name ?? p.Country).Contains(search)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.Title)
            .ThenBy(p => p.Country)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * Paging.DefaultPageSize)
            .Take(Paging.DefaultPageSize)
            .Select(p => new ParticipantRow(
                p.Id,
                p.Title,
                p.FirstName,
                p.LastName,
                p.Country,
                p.CountryRef!.NiceName ?? p.CountryRef.Name ?? p.Country
            ))
            .ToListAsync(cancellationToken);
        return new PagedResult<ParticipantRow>(items, total, page, Paging.DefaultPageSize);
    }

    public async Task<ParticipantInfo?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Participants.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ParticipantInfo(
                p.Id,
                p.Title,
                p.FirstName,
                p.LastName,
                p.Country,
                p.CountryRef!.NiceName ?? p.CountryRef.Name ?? p.Country,
                Array.Empty<string>(),
                Array.Empty<ParticipantRoundWin>(),
                Array.Empty<ParticipantSeasonWin>()
            ))
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var roundSports =
            from winner in db.RoundWinners.AsNoTracking()
            where winner.Participant == id
            join round in db.Rounds.AsNoTracking() on winner.Round equals round.Id
            select round.SeasonRef!.Sport;
        var seasonSports =
            from winner in db.SeasonWinners.AsNoTracking()
            where winner.Participant == id
            join season in db.Seasons.AsNoTracking() on winner.Season equals season.Id
            select season.Sport;
        var sports = await roundSports.Union(seasonSports).OrderBy(sport => sport).ToArrayAsync(cancellationToken);

        var roundWins = await (
            from winner in db.RoundWinners.AsNoTracking()
            where winner.Participant == id
            join round in db.Rounds.AsNoTracking() on winner.Round equals round.Id
            join season in db.Seasons.AsNoTracking() on round.Season equals season.Id
            select new
            {
                RoundId = round.Id,
                round.Date,
                season.Sport,
                SeasonLabel = season.Label,
                round.Number,
                round.Name,
                round.Venue,
                VenueCountry = db
                    .Venues.Where(venue => venue.Name == round.Venue)
                    .Select(venue => venue.Country)
                    .FirstOrDefault(),
                round.Rating,
            }
        )
            .Distinct()
            .OrderByDescending(round => round.Date)
            .ThenByDescending(round => round.RoundId)
            .Select(round => new ParticipantRoundWin(
                round.RoundId,
                round.Date,
                round.Sport,
                round.SeasonLabel,
                round.Number,
                round.Name,
                round.Venue,
                round.VenueCountry ?? "",
                round.Rating
            ))
            .ToArrayAsync(cancellationToken);

        var seasonWins = await db
            .Seasons.AsNoTracking()
            .Where(season => db.SeasonWinners.Any(winner => winner.Season == season.Id && winner.Participant == id))
            .Select(season => new ParticipantSeasonWin(
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

        return row with
        {
            Sports = sports,
            RoundWins = roundWins,
            SeasonWins = seasonWins,
        };
    }

    public async Task<ParticipantForm?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db
            .Participants.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Title,
                p.FirstName,
                p.LastName,
                p.Country,
            })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new ParticipantForm
            {
                Title = row.Title,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Country = row.Country,
                RowVersion = RowVersion.Compute(row.Title, row.FirstName, row.LastName, row.Country),
            };
    }

    public async Task<Result<int>> CreateAsync(ParticipantForm form, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var title = form.Title?.Trim() ?? "";
        var first = form.FirstName!.Trim();
        var last = form.LastName!.Trim();
        // Unique index plus serializable isolation: trim-equal Title/First/Last/Country cannot race in.
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var country = await CanonicalCountryAsync(db, form.Country!, ct);
                if (country is null)
                {
                    return Error.Validation(nameof(ParticipantForm.Country), "Select a valid country.");
                }

                if (
                    await db.Participants.AnyAsync(
                        p => p.Title == title && p.FirstName == first && p.LastName == last && p.Country == country,
                        ct
                    )
                )
                {
                    return Error.Validation("", DuplicateMessage);
                }

                var participant = new Participant
                {
                    Title = title,
                    FirstName = first,
                    LastName = last,
                    Country = country,
                };
                db.Participants.Add(participant);
                await db.SaveChangesAsync(ct);
                return Result<int>.Ok(participant.Id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Participants);
        }

        return result;
    }

    public async Task<Result<int>> UpdateAsync(
        int id,
        ParticipantForm form,
        CancellationToken cancellationToken = default
    )
    {
        var validation = await validator.ValidateAsync(form, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var title = form.Title?.Trim() ?? "";
        var first = form.FirstName!.Trim();
        var last = form.LastName!.Trim();
        var result = await factory.InTransactionAsync(
            async (db, ct) =>
            {
                var country = await CanonicalCountryAsync(db, form.Country!, ct);
                if (country is null)
                {
                    return Error.Validation(nameof(ParticipantForm.Country), "Select a valid country.");
                }

                var rows = await db
                    .Participants.FromSql($"SELECT * FROM `Participant` WHERE `Id` = {id} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(ct);
                if (rows.Count == 0)
                {
                    return Error.NotFound("Participant not found.");
                }

                var current = rows[0];
                var version = RowVersion.Compute(current.Title, current.FirstName, current.LastName, current.Country);
                if (!RowVersion.Matches(form.RowVersion, version))
                {
                    return Error.Conflict(StaleMessage);
                }

                if (
                    await db.Participants.AnyAsync(
                        p =>
                            p.Id != id
                            && p.Title == title
                            && p.FirstName == first
                            && p.LastName == last
                            && p.Country == country,
                        ct
                    )
                )
                {
                    return Error.Validation("", DuplicateMessage);
                }

                await db
                    .Participants.Where(p => p.Id == id)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(p => p.Title, title)
                                .SetProperty(p => p.FirstName, first)
                                .SetProperty(p => p.LastName, last)
                                .SetProperty(p => p.Country, country),
                        ct
                    );
                return Result<int>.Ok(id);
            },
            IsolationLevel.Serializable,
            cancellationToken
        );
        if (result.IsSuccess)
        {
            cache.Invalidate(LookupKind.Participants);
        }

        return result;
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
}
