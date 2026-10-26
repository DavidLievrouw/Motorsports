using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Motorsports.Common;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure.Auth;

/// <summary>
/// Identity store over the legacy <c>User</c> table. One context per operation. Deleted users are invisible, so they
/// can neither sign in nor keep a session. Only the columns the new app owns are ever written.
/// </summary>
public sealed class UserStore(IDbContextFactory<MotorsportsDbContext> factory)
    : IUserPasswordStore<User>,
        IUserSecurityStampStore<User>
{
    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.Value);

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Username);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? "";
        return Task.CompletedTask;
    }

    // The table has no normalized column; the column collation is case-insensitive.
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Username);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Users are managed outside the application.");

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Users are managed outside the application.");

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var previousStamp = user.ConcurrencyStamp;
        var newStamp = Guid.NewGuid().ToString("N");

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var rows = await context
            .Users.Where(u => u.Id == user.Id && !u.IsDeleted && u.ConcurrencyStamp == previousStamp)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(u => u.PasswordHashV3, user.PasswordHashV3)
                        .SetProperty(u => u.SecurityStamp, user.SecurityStamp)
                        .SetProperty(u => u.ForceChangePassword, user.ForceChangePassword)
                        .SetProperty(u => u.ConcurrencyStamp, newStamp),
                cancellationToken
            );

        if (rows == 0)
        {
            return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
        }

        user.ConcurrencyStamp = newStamp;
        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId.From(userId) && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalizedUserName && !u.IsDeleted, cancellationToken);
    }

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHashV3 = passwordHash;
        return Task.CompletedTask;
    }

    // Users without a v3 hash expose the marker so the hasher verifies the legacy PBKDF2 columns.
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(
            !string.IsNullOrEmpty(user.PasswordHashV3) ? user.PasswordHashV3
            : user.PasswordHash.Length > 0 ? LegacyPasswordHasher.LegacyMarker
            : null
        );

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHashV3) || user.PasswordHash.Length > 0);

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    // Legacy rows have no stamp yet; an empty one stays consistent between cookie issue and validation.
    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.SecurityStamp ?? "");

    public void Dispose() { }
}
