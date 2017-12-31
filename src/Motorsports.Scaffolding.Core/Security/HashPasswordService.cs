using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Motorsports.Scaffolding.Core.Security {
  public interface IHashPasswordService {
    HashedPassword Hash(PlainTextPassword password);
    HashedPassword Hash(PlainTextPassword password, byte[] salt, KeyDerivationPrf prf, int iterations);
  }

  public class HashPasswordService : IHashPasswordService {
    public delegate HashedPassword Hasher(PlainTextPassword password);

    public delegate byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterations, int hashSize);

    public delegate byte[] SaltGenerator();

    readonly SaltGenerator _generateSalt;
    readonly int _hashSize;
    readonly int _iterations;
    readonly Pbkdf2 _pbkdf2;
    readonly KeyDerivationPrf _prf;

    public HashPasswordService(PasswordHashingConfig passwordHashingConfig) {
      if (passwordHashingConfig == null) throw new ArgumentNullException(nameof(passwordHashingConfig));
      if (!Enum.IsDefined(typeof(KeyDerivationPrf), passwordHashingConfig.KeyDerivationPrf)) {
        throw new ArgumentOutOfRangeException(
          nameof(passwordHashingConfig.KeyDerivationPrf),
          "Value should be defined in the KeyDerivationPrf enum.");
      }

      _generateSalt = CreateSaltGenerator(passwordHashingConfig.SaltSizeInBytes);
      _pbkdf2 = KeyDerivation.Pbkdf2;
      _prf = passwordHashingConfig.KeyDerivationPrf;
      _iterations = passwordHashingConfig.Iterations;
      _hashSize = passwordHashingConfig.HashSizeInBytes;
    }

    public HashedPassword Hash(PlainTextPassword password) {
      return Hash(password, _generateSalt(), _prf, _iterations);
    }

    public HashedPassword Hash(PlainTextPassword password, byte[] salt, KeyDerivationPrf prf, int iterations) {
      var hash = _pbkdf2(
        password,
        salt,
        prf,
        iterations,
        _hashSize
      );

      return new HashedPassword(hash, salt, iterations, prf);
    }

    static SaltGenerator CreateSaltGenerator(int saltSize) {
      return () => {
        var salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create()) {
          rng.GetBytes(salt);
        }

        return salt;
      };
    }
  }
}