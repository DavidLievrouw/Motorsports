using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure.Auth;

/// <summary>
/// Verifies the legacy PBKDF2 hash (HMACSHA1/256/512, per-user salt and iterations) until the user has an
/// Identity v3 hash, then asks Identity to rehash on the first successful legacy login.
/// </summary>
public sealed class LegacyPasswordHasher : IPasswordHasher<User>
{
    /// <summary>Stand-in returned by the store for users that only have a legacy hash.</summary>
    public const string LegacyMarker = "legacy-pbkdf2";

    private readonly PasswordHasher<User> _v3 = new();

    public string HashPassword(User user, string password) => _v3.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword == LegacyMarker)
        {
            return VerifyLegacy(user, providedPassword)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        return string.IsNullOrEmpty(hashedPassword)
            ? PasswordVerificationResult.Failed
            : _v3.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }

    public static bool VerifyLegacy(User user, string password)
    {
        if (user.PasswordHash.Length == 0 || user.Salt.Length == 0 || user.Iterations <= 0)
        {
            return false;
        }

        if (!TryGetAlgorithm(user.Prf, out var algorithm))
        {
            return false;
        }

        // Legacy KeyDerivation.Pbkdf2 encodes the string as UTF-8; the output length is the stored hash length.
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            user.Salt,
            user.Iterations,
            algorithm,
            user.PasswordHash.Length
        );
        return CryptographicOperations.FixedTimeEquals(actual, user.PasswordHash);
    }

    private static bool TryGetAlgorithm(string? prf, out HashAlgorithmName algorithm)
    {
        algorithm = default;
        if (string.Equals(prf, "HMACSHA1", StringComparison.OrdinalIgnoreCase))
        {
            algorithm = HashAlgorithmName.SHA1;
        }
        else if (string.Equals(prf, "HMACSHA256", StringComparison.OrdinalIgnoreCase))
        {
            algorithm = HashAlgorithmName.SHA256;
        }
        else if (string.Equals(prf, "HMACSHA512", StringComparison.OrdinalIgnoreCase))
        {
            algorithm = HashAlgorithmName.SHA512;
        }
        else
        {
            return false;
        }

        return true;
    }
}
