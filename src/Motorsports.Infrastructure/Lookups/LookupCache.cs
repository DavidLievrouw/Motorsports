using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Motorsports.Infrastructure.Lookups;

public enum LookupKind
{
    Countries,
    Sports,
}

public sealed class LookupCacheOptions
{
    public const string Section = "Cache";

    /// <summary>The legacy app writes to the same DB and cannot invalidate us, so entries expire.</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed record CountryOption(string Iso, string Name);

public sealed record SportOption(string Name, string? FullName);

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
                    .Select(c => new CountryOption(c.Iso, c.NiceName ?? c.Name ?? c.Iso))
                    .ToListAsync(ct);
                rows.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
                return [.. rows];
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
    }

    /// <summary>Countries sorted by display name.</summary>
    public ValueTask<CountryOption[]> GetCountriesAsync(CancellationToken cancellationToken = default) =>
        countries.GetAsync(null, cancellationToken);

    public ValueTask<SportOption[]> GetSportsAsync(CancellationToken cancellationToken = default) =>
        sports.GetAsync(null, cancellationToken);

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
