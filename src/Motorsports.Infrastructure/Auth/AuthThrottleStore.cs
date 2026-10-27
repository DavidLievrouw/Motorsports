using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motorsports.Common;
using MySqlConnector;

namespace Motorsports.Infrastructure.Auth;

public sealed record AuthAdmission(int RetryAfterSeconds, AuthReservation? Reservation = null);

public sealed record AuthReservation(string[] Keys, DateTime AccountWindow);

public enum AuthAttemptOutcome
{
    Failed,
    Succeeded,
    InvalidInput,
}

public sealed class AuthThrottleStore(
    IDbContextFactory<MotorsportsDbContext> factory,
    IOptions<AuthProtectionOptions> options,
    TimeProvider clock,
    ILogger<AuthThrottleStore> logger
)
{
    private readonly AuthProtectionOptions _limits = options.Value;

    public static string Key(string kind, string value) =>
        $"{kind}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))}";

    public async Task<Result<AuthAdmission>> ReserveAsync(
        string normalizedUsername,
        string address,
        string device,
        CancellationToken cancellationToken
    ) =>
        await RunAsync<AuthAdmission>(
            async (db, ct) =>
            {
                var global = await LockGlobalAsync(db, ct);
                var now = UtcNow();
                ResetWindow(global, now, _limits.GlobalWindowSeconds);
                if (global.Attempts >= _limits.GlobalAttempts)
                {
                    return Result<AuthAdmission>.Ok(
                        new AuthAdmission(SecondsUntil(global.WindowStart.AddSeconds(_limits.GlobalWindowSeconds), now))
                    );
                }

                // Charge the global budget even when an account/source is already blocked. This bounds DB work
                // and key creation during floods; only the singleton row is read once that budget is exhausted.
                global.Attempts++;
                await CleanupExpiredAsync(db, global, now, ct);

                // MariaDB's username collation also equates accents and trailing spaces. Key existing accounts
                // by their immutable ID, not merely by an uppercased spelling of the submitted username.
                var userIds = await db
                    .Users.AsNoTracking()
                    .Where(user => user.Username == normalizedUsername && !user.IsDeleted)
                    .Select(user => user.Id)
                    .Take(1)
                    .ToArrayAsync(ct);
                var accountKey =
                    userIds.Length == 0
                        ? Key("account", $"name:{normalizedUsername}")
                        : Key("account", $"id:{userIds[0].Value}");
                string[] keys = [accountKey, Key("ip", address), Key("device", device)];
                var stored = await db
                    .AuthThrottleStates.Where(row => Enumerable.Contains(keys, row.Key))
                    .ToArrayAsync(ct);
                var states = new AuthThrottleState[keys.Length];
                var retry = 0;
                for (var index = 0; index < keys.Length; index++)
                {
                    var state = Array.Find(stored, row => row.Key == keys[index]);
                    if (state is null)
                    {
                        state = new AuthThrottleState { Key = keys[index], WindowStart = now };
                    }
                    else if (state.ExpiresAt <= now)
                    {
                        state.Attempts = 0;
                        state.Failures = 0;
                        state.AccountChanges = 0;
                        state.LastAccount = null;
                        state.LastSuccess = null;
                        state.BlockedUntil = DateTime.UnixEpoch;
                        state.WindowStart = now;
                    }
                    if (state.LastFailure.AddSeconds(_limits.AccountWindowSeconds) <= now)
                    {
                        state.Failures = 0;
                    }
                    var window = index == 0 ? _limits.AccountWindowSeconds : _limits.SourceWindowSeconds;
                    ResetWindow(state, now, window);
                    var permits = index switch
                    {
                        0 => _limits.AccountFailures,
                        1 => _limits.IpAttempts,
                        _ => _limits.DeviceAttempts,
                    };
                    if (state.Attempts >= permits)
                    {
                        retry = Math.Max(retry, SecondsUntil(state.WindowStart.AddSeconds(window), now));
                    }
                    retry = Math.Max(retry, SecondsUntil(state.BlockedUntil, now));
                    states[index] = state;
                }

                if (retry == 0)
                {
                    for (var index = 0; index < states.Length; index++)
                    {
                        var state = states[index];
                        if (db.Entry(state).State == EntityState.Detached)
                        {
                            db.AuthThrottleStates.Add(state);
                        }
                        state.Attempts++;
                        state.ExpiresAt = now.AddHours(_limits.ReputationHours);
                        if (index > 0)
                        {
                            if (state.LastAccount is not null && state.LastAccount != accountKey)
                            {
                                state.AccountChanges = Math.Min(100, state.AccountChanges + 1);
                            }
                            state.LastAccount = accountKey;
                        }
                    }
                }

                await db.SaveChangesAsync(ct);
                return Result<AuthAdmission>.Ok(
                    new AuthAdmission(retry, retry == 0 ? new AuthReservation(keys, states[0].WindowStart) : null)
                );
            },
            cancellationToken
        );

    public async Task<Result<Unit>> CompleteAsync(
        AuthReservation reservation,
        AuthAttemptOutcome outcome,
        CancellationToken cancellationToken
    ) =>
        await RunAsync(
            async (db, ct) =>
            {
                await LockGlobalAsync(db, ct);
                var now = UtcNow();
                var states = await db
                    .AuthThrottleStates.Where(row => Enumerable.Contains(reservation.Keys, row.Key))
                    .ToArrayAsync(ct);
                foreach (var state in states)
                {
                    var account = state.Key == reservation.Keys[0];
                    if (outcome == AuthAttemptOutcome.Succeeded)
                    {
                        state.LastSuccess = now;
                        // Successful logins consume source/global permits, not the failed-account budget.
                        // Do not wipe other concurrent failures or cooldowns on a successful request.
                        if (account && state.WindowStart == reservation.AccountWindow)
                        {
                            state.Attempts = Math.Max(0, state.Attempts - 1);
                        }
                    }
                    else if (outcome == AuthAttemptOutcome.Failed)
                    {
                        if (state.LastFailure.AddSeconds(_limits.AccountWindowSeconds) <= now)
                        {
                            state.Failures = 0;
                        }
                        state.LastFailure = now;
                        state.Failures = Math.Min(100, state.Failures + 1);
                        var suspiciousSource = !account && state.AccountChanges >= 4 && state.Failures >= 3;
                        var unprovenSource =
                            !account
                            && (
                                state.LastSuccess is null
                                || state.LastSuccess.Value.AddHours(_limits.ReputationHours) <= now
                            );
                        var exponent =
                            state.Failures - (account || unprovenSource ? 3 : 4) + (suspiciousSource ? 2 : 0);
                        if (exponent >= 0)
                        {
                            var seconds = Math.Min(_limits.MaximumCooldownSeconds, 1 << Math.Min(16, exponent));
                            state.BlockedUntil = now.AddSeconds(seconds);
                        }
                    }
                    state.ExpiresAt = now.AddHours(_limits.ReputationHours);
                }
                if (states.Length != reservation.Keys.Length)
                {
                    logger.LogWarning("Authentication protection reservation was removed before completion.");
                    return Error.Conflict("Authentication protection state changed. Retry later.");
                }
                await db.SaveChangesAsync(ct);
                return Result.Ok();
            },
            cancellationToken
        );

    public Task<Result<Unit>> CleanupAsync(CancellationToken cancellationToken) =>
        RunAsync(
            async (db, ct) =>
            {
                var global = await LockGlobalAsync(db, ct);
                await CleanupExpiredAsync(db, global, UtcNow(), ct);
                await db.SaveChangesAsync(ct);
                return Result.Ok();
            },
            cancellationToken
        );

    private static async Task CleanupExpiredAsync(
        MotorsportsDbContext db,
        AuthThrottleState global,
        DateTime now,
        CancellationToken ct
    )
    {
        if (global.ExpiresAt > now)
        {
            return;
        }
        var expiredKeys = await db
            .AuthThrottleStates.Where(row => row.Key != "global" && row.ExpiresAt <= now)
            .OrderBy(row => row.ExpiresAt)
            .Select(row => row.Key)
            .Take(500)
            .ToArrayAsync(ct);
        if (expiredKeys.Length != 0)
        {
            await db.AuthThrottleStates.Where(row => Enumerable.Contains(expiredKeys, row.Key)).ExecuteDeleteAsync(ct);
        }
        global.ExpiresAt = now.AddMinutes(1);
    }

    private async Task<Result<T>> RunAsync<T>(
        Func<MotorsportsDbContext, CancellationToken, Task<Result<T>>> work,
        CancellationToken ct
    )
    {
        try
        {
            return await factory.InTransactionAsync(work, cancellationToken: ct);
        }
        catch (Exception exception) when (exception is MySqlException or DbUpdateException)
        {
            logger.LogError(exception, "Authentication protection database operation failed; authentication denied.");
            return Error.Conflict("Authentication protection is unavailable.");
        }
    }

    private static async Task<AuthThrottleState> LockGlobalAsync(MotorsportsDbContext db, CancellationToken ct)
    {
        var rows = await db
            .AuthThrottleStates.FromSql($"SELECT * FROM `AuthThrottleState` WHERE `Key` = 'global' FOR UPDATE")
            .ToListAsync(ct);
        return rows.Single();
    }

    private static void ResetWindow(AuthThrottleState state, DateTime now, int seconds)
    {
        if (state.WindowStart.AddSeconds(seconds) <= now)
        {
            state.WindowStart = now;
            state.Attempts = 0;
        }
    }

    private static int SecondsUntil(DateTime until, DateTime now) =>
        until <= now ? 0 : Math.Max(1, (int)Math.Ceiling((until - now).TotalSeconds));

    private DateTime UtcNow()
    {
        var ticks = clock.GetUtcNow().UtcTicks;
        // MariaDB datetime(6) persists microseconds, not .NET's 100 ns ticks.
        return new DateTime(ticks - ticks % 10, DateTimeKind.Utc);
    }
}
