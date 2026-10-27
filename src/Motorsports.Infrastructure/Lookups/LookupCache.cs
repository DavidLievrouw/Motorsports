using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motorsports.Common;

namespace Motorsports.Infrastructure.Lookups;

public enum LookupKind
{
    Countries,
    Sports,
    Participants,
    Teams,
    SeasonEntries,
    Venues,
    Statuses,
}

public sealed class LookupCacheOptions
{
    public const string Section = "Cache";

    /// <summary>Entries expire so external database changes become visible without application invalidation.</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed record CountryOption(CountryIso Iso, string Name);

public sealed record SportOption(SportName Name, string? FullName);

public sealed record ParticipantOption(
    ParticipantId Id,
    string FirstName,
    string LastName,
    CountryIso Country,
    string CountryName
);

public sealed record TeamHistoryOption(SeasonId SeasonId, string? SeasonLabel, string Name, DateOnly? StartDate);

public sealed record TeamOption(
    TeamId Id,
    string Name,
    SportName Sport,
    CountryIso Country,
    string CountryName,
    TeamHistoryOption[] History
)
{
    public string LatestEntryName => History.Length == 0 ? Name : History[0].Name;
}

public sealed record SeasonEntryOption(
    SeasonId SeasonId,
    TeamId TeamId,
    string Name,
    string? SeasonLabel,
    SportName Sport,
    string TeamName,
    CountryIso Country,
    string CountryName
);

public sealed record VenueOption(VenueName Name, CountryIso Country, string CountryName);

public sealed record StatusOption(StatusName Name, sbyte Step);

/// <summary>
/// Shared, immutable lookup lists (one query per kind). Writers call <see cref="Invalidate"/> after commit: the next read
/// reloads synchronously. Entries older than <c>MaxAge</c> are served stale and refreshed in the background; forms
/// call <see cref="RevalidateAsync"/> to reload anything older than <see cref="FormRevalidateAge"/>.
/// </summary>
public sealed class LookupCache
{
    public static readonly TimeSpan FormRevalidateAge = TimeSpan.FromSeconds(5);

    private readonly Slot<CountryOption> countries;
    private readonly Slot<SportOption> sports;
    private readonly Slot<ParticipantOption> participants;
    private readonly Slot<TeamOption> teams;
    private readonly Slot<SeasonEntryOption> seasonEntries;
    private readonly Slot<VenueOption> venues;
    private readonly Slot<StatusOption> statuses;

    public LookupCache(
        IDbContextFactory<MotorsportsDbContext> factory,
        TimeProvider time,
        IOptions<LookupCacheOptions> options,
        ILogger<LookupCache> logger
    )
    {
        var maxAge = options.Value.MaxAge;
        countries = new Slot<CountryOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await context
                    .Countries.AsNoTracking()
                    .Select(c => new
                    {
                        c.Iso,
                        c.NiceName,
                        c.Name,
                    })
                    .ToArrayAsync(ct);
                return rows.Select(c => new CountryOption(c.Iso, c.NiceName ?? c.Name ?? c.Iso.Value))
                    .OrderBy(static country => country.Name, StringComparer.Ordinal)
                    .ToArray();
            },
            time,
            maxAge,
            logger
        );
        sports = new Slot<SportOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await context
                    .Sports.AsNoTracking()
                    .OrderBy(s => s.Name)
                    .Select(s => new SportOption(s.Name, s.FullName))
                    .ToListAsync(ct);
                return [.. rows];
            },
            time,
            maxAge,
            logger
        );
        participants = new Slot<ParticipantOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await context
                    .Participants.AsNoTracking()
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ThenBy(p => p.Title)
                    .ThenBy(p => p.Country)
                    .Select(p => new
                    {
                        p.Id,
                        p.FirstName,
                        p.LastName,
                        p.Country,
                        CountryName = p.CountryRef!.NiceName ?? p.CountryRef.Name,
                    })
                    .ToArrayAsync(ct);
                return rows.Select(p => new ParticipantOption(
                        p.Id,
                        p.FirstName,
                        p.LastName,
                        p.Country,
                        p.CountryName ?? p.Country.Value
                    ))
                    .ToArray();
            },
            time,
            maxAge,
            logger
        );
        teams = new Slot<TeamOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await (
                    from team in context.Teams.AsNoTracking()
                    join entry in context.SeasonEntries.AsNoTracking() on team.Id equals entry.Team into entries
                    from entry in entries.DefaultIfEmpty()
                    select new
                    {
                        team.Id,
                        team.Name,
                        Sport = team.SportRef!.Name,
                        team.Country,
                        CountryName = team.CountryRef!.NiceName ?? team.CountryRef.Name,
                        SeasonId = (SeasonId?)entry!.Season,
                        SeasonLabel = entry.SeasonRef!.Label,
                        EntryName = entry.Name,
                        StartDate = context
                            .Rounds.Where(round => round.Season == entry.Season)
                            .Select(round => (DateOnly?)round.Date)
                            .Min(),
                        EndDate = context
                            .Rounds.Where(round => round.Season == entry.Season)
                            .Select(round => (DateOnly?)round.Date)
                            .Max(),
                    }
                ).ToListAsync(ct);
                return rows.GroupBy(row => new
                    {
                        row.Id,
                        row.Name,
                        row.Sport,
                        row.Country,
                        row.CountryName,
                    })
                    .Select(group => new TeamOption(
                        group.Key.Id,
                        group.Key.Name,
                        group.Key.Sport,
                        group.Key.Country,
                        group.Key.CountryName ?? group.Key.Country.Value,
                        group
                            .Where(row => row.SeasonId.HasValue)
                            .Select(row => new TeamHistoryOption(
                                row.SeasonId!.Value,
                                !string.IsNullOrWhiteSpace(row.SeasonLabel)
                                    ? row.SeasonLabel
                                    : row.EndDate?.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                row.EntryName!,
                                row.StartDate
                            ))
                            .OrderByDescending(entry => entry.StartDate is null)
                            .ThenByDescending(entry => entry.StartDate)
                            .ThenByDescending(entry => entry.SeasonId.Value)
                            .ToArray()
                    ))
                    .OrderBy(team => team.Name, StringComparer.Ordinal)
                    .ToArray();
            },
            time,
            maxAge,
            logger
        );
        seasonEntries = new Slot<SeasonEntryOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await context
                    .SeasonEntries.AsNoTracking()
                    .OrderBy(entry => entry.Name)
                    .Select(entry => new
                    {
                        entry.Season,
                        entry.Team,
                        entry.Name,
                        SeasonLabel = entry.SeasonRef!.Label,
                        Sport = entry.SeasonRef.Sport,
                        TeamName = entry.TeamRef!.Name,
                        Country = entry.TeamRef.Country,
                        CountryName = entry.TeamRef.CountryRef!.NiceName ?? entry.TeamRef.CountryRef.Name,
                    })
                    .ToArrayAsync(ct);
                return rows.Select(entry => new SeasonEntryOption(
                        entry.Season,
                        entry.Team,
                        entry.Name,
                        entry.SeasonLabel,
                        entry.Sport,
                        entry.TeamName,
                        entry.Country,
                        entry.CountryName ?? entry.Country.Value
                    ))
                    .ToArray();
            },
            time,
            maxAge,
            logger
        );
        venues = new Slot<VenueOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                var rows = await context
                    .Venues.AsNoTracking()
                    .OrderBy(venue => venue.Name)
                    .Select(venue => new
                    {
                        venue.Name,
                        venue.Country,
                        CountryName = venue.CountryRef!.NiceName ?? venue.CountryRef.Name,
                    })
                    .ToArrayAsync(ct);
                return rows.Select(venue => new VenueOption(
                        venue.Name,
                        venue.Country,
                        venue.CountryName ?? venue.Country.Value
                    ))
                    .ToArray();
            },
            time,
            maxAge,
            logger
        );
        statuses = new Slot<StatusOption>(
            async ct =>
            {
                await using var context = await factory.CreateDbContextAsync(ct);
                return await context
                    .Statuses.AsNoTracking()
                    .OrderBy(status => status.Step)
                    .ThenBy(status => status.Name)
                    .Select(status => new StatusOption(status.Name, status.Step))
                    .ToArrayAsync(ct);
            },
            time,
            maxAge,
            logger
        );
    }

    /// <summary>Countries sorted by display name.</summary>
    public ValueTask<CountryOption[]> GetCountriesAsync(CancellationToken cancellationToken = default) =>
        countries.GetAsync(null, cancellationToken);

    public ValueTask<SportOption[]> GetSportsAsync(CancellationToken cancellationToken = default) =>
        sports.GetAsync(null, cancellationToken);

    public ValueTask<ParticipantOption[]> GetParticipantsAsync(CancellationToken cancellationToken = default) =>
        participants.GetAsync(null, cancellationToken);

    public ValueTask<TeamOption[]> GetTeamsAsync(CancellationToken cancellationToken = default) =>
        teams.GetAsync(null, cancellationToken);

    public ValueTask<SeasonEntryOption[]> GetSeasonEntriesAsync(CancellationToken cancellationToken = default) =>
        seasonEntries.GetAsync(null, cancellationToken);

    public ValueTask<VenueOption[]> GetVenuesAsync(CancellationToken cancellationToken = default) =>
        venues.GetAsync(null, cancellationToken);

    public ValueTask<StatusOption[]> GetStatusesAsync(CancellationToken cancellationToken = default) =>
        statuses.GetAsync(null, cancellationToken);

    /// <summary>Reloads the kinds a form needs when their data is older than <see cref="FormRevalidateAge"/>.</summary>
    public async Task RevalidateAsync(LookupKind kind, CancellationToken cancellationToken = default)
    {
        switch (kind)
        {
            case LookupKind.Countries:
                await countries.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.Sports:
                await sports.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.Participants:
                await participants.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.Teams:
                await teams.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.SeasonEntries:
                await seasonEntries.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.Venues:
                await venues.GetAsync(FormRevalidateAge, cancellationToken);
                break;
            case LookupKind.Statuses:
                await statuses.GetAsync(FormRevalidateAge, cancellationToken);
                break;
        }
    }

    public void Invalidate(LookupKind kind)
    {
        switch (kind)
        {
            case LookupKind.Countries:
                countries.Invalidate();
                break;
            case LookupKind.Sports:
                sports.Invalidate();
                break;
            case LookupKind.Participants:
                participants.Invalidate();
                break;
            case LookupKind.Teams:
                teams.Invalidate();
                break;
            case LookupKind.SeasonEntries:
                seasonEntries.Invalidate();
                break;
            case LookupKind.Venues:
                venues.Invalidate();
                break;
            case LookupKind.Statuses:
                statuses.Invalidate();
                break;
        }
    }

    private sealed class Slot<T>(
        Func<CancellationToken, Task<T[]>> load,
        TimeProvider time,
        TimeSpan maxAge,
        ILogger logger
    )
    {
        private sealed record Snapshot(T[] Data, long Timestamp, int Version);

        private readonly SemaphoreSlim gate = new(1, 1);
        private Snapshot? current;
        private int version;
        private int backgroundRunning;

        public void Invalidate() => Interlocked.Increment(ref version);

        public async ValueTask<T[]> GetAsync(TimeSpan? revalidateAfter, CancellationToken cancellationToken)
        {
            var snapshot = Volatile.Read(ref current);
            if (snapshot is null || snapshot.Version != Volatile.Read(ref version))
            {
                return (await RefreshAsync(time.GetTimestamp(), cancellationToken)).Data;
            }

            var age = time.GetElapsedTime(snapshot.Timestamp);
            if (revalidateAfter is { } limit && age > limit)
            {
                return (await RefreshAsync(time.GetTimestamp(), cancellationToken)).Data;
            }

            if (age > maxAge && Interlocked.CompareExchange(ref backgroundRunning, 1, 0) == 0)
            {
                _ = Task.Run(() => BackgroundRefreshAsync(time.GetTimestamp()), CancellationToken.None);
            }

            return snapshot.Data;
        }

        private async Task BackgroundRefreshAsync(long requestedAt)
        {
            try
            {
                await RefreshAsync(requestedAt, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background lookup refresh failed; serving stale data.");
            }
            finally
            {
                Volatile.Write(ref backgroundRunning, 0);
            }
        }

        private async Task<Snapshot> RefreshAsync(long requestedAt, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                // Another caller may have refreshed while we waited for the gate.
                var existing = Volatile.Read(ref current);
                if (
                    existing is not null
                    && existing.Version == Volatile.Read(ref version)
                    && existing.Timestamp >= requestedAt
                )
                {
                    return existing;
                }

                // Read the version first: an invalidation during the query leaves the new snapshot marked stale.
                var loadVersion = Volatile.Read(ref version);
                var startedAt = time.GetTimestamp();
                var data = await load(cancellationToken);
                var fresh = new Snapshot(data, startedAt, loadVersion);
                Interlocked.Exchange(ref current, fresh);
                return fresh;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}

public static class LookupCacheExtensions
{
    public static IServiceCollection AddLookupCache(this IServiceCollection services)
    {
        services.AddOptions<LookupCacheOptions>();
        services.AddSingleton<LookupCache>();
        return services;
    }
}
