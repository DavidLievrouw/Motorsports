using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Motorsports.Common;
using Motorsports.Infrastructure.Auth;
using Motorsports.Infrastructure.Entities;
using Shouldly;

namespace Motorsports.UnitTests.Auth;

public class LegacyPasswordHasherTests
{
    private readonly LegacyPasswordHasher _hasher = new();

    private static User LegacyUser(string prf, HashAlgorithmName algorithm, string password, int iterations = 1000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return new User
        {
            Id = UserId.From(Guid.NewGuid().ToString()),
            Username = "legacy",
            Prf = prf,
            Iterations = iterations,
            Salt = salt,
            PasswordHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, algorithm, 32),
        };
    }

    [Theory]
    [InlineData("HMACSHA1")]
    [InlineData("HMACSHA256")]
    [InlineData("HMACSHA512")]
    public void Legacy_hash_verifies_and_asks_for_rehash(string prf)
    {
        var algorithm = new HashAlgorithmName(prf["HMAC".Length..]);
        var user = LegacyUser(prf, algorithm, "pässword €1");

        _hasher
            .VerifyHashedPassword(user, LegacyPasswordHasher.LegacyMarker, "pässword €1")
            .ShouldBe(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("")]
    [InlineData("PASSWORD")]
    public void Wrong_password_fails(string attempt)
    {
        var user = LegacyUser("HMACSHA512", HashAlgorithmName.SHA512, "password");

        _hasher
            .VerifyHashedPassword(user, LegacyPasswordHasher.LegacyMarker, attempt)
            .ShouldBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Unknown_prf_or_missing_data_fails_instead_of_throwing()
    {
        var user = LegacyUser("HMACSHA512", HashAlgorithmName.SHA512, "password");

        user.Prf = "MD5";
        LegacyPasswordHasher.VerifyLegacy(user, "password").ShouldBeFalse();

        user.Prf = "HMACSHA512";
        user.Iterations = 0;
        LegacyPasswordHasher.VerifyLegacy(user, "password").ShouldBeFalse();

        user.Iterations = 1000;
        user.Salt = [];
        LegacyPasswordHasher.VerifyLegacy(user, "password").ShouldBeFalse();
    }

    [Fact]
    public void V3_hash_round_trips_without_rehash_and_ignores_legacy_columns()
    {
        var user = LegacyUser("HMACSHA512", HashAlgorithmName.SHA512, "old password");
        var v3 = _hasher.HashPassword(user, "new password");

        _hasher.VerifyHashedPassword(user, v3, "new password").ShouldBe(PasswordVerificationResult.Success);
        _hasher.VerifyHashedPassword(user, v3, "old password").ShouldBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Empty_stored_hash_fails()
    {
        _hasher.VerifyHashedPassword(new User(), "", "x").ShouldBe(PasswordVerificationResult.Failed);
    }
}
