using System;
using System.Linq;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Motorsports.Scaffolding.Core.Security {
  public class HashedPassword {
    public HashedPassword(byte[] hash, byte[] salt, int iterations, KeyDerivationPrf prf) {
      Hash = hash;
      Salt = salt;
      Iterations = iterations;
      Prf = prf;
    }

    public byte[] Hash { get; }
    public byte[] Salt { get; }
    public int Iterations { get; }
    public KeyDerivationPrf Prf { get; }

    public static string PrfName(KeyDerivationPrf prf) {
      return Enum.GetName(typeof(KeyDerivationPrf), prf);
    }

    public override bool Equals(object obj) {
      return Equals(obj as HashedPassword);
    }

    protected bool Equals(HashedPassword other) {
      if (ReferenceEquals(null, this)) return ReferenceEquals(null, other);
      if (ReferenceEquals(null, other)) return false;
      return Hash.SequenceEqual(other.Hash) &&
             Salt.SequenceEqual(other.Salt) &&
             Iterations == other.Iterations &&
             Equals(Prf, other.Prf);
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = Hash != null
          ? Hash.GetHashCode()
          : 0;
        hashCode = (hashCode * 397) ^
                   (Salt != null
                     ? Salt.GetHashCode()
                     : 0);
        hashCode = (hashCode * 397) ^ Iterations;
        hashCode = (hashCode * 397) ^ Prf.GetHashCode();
        return hashCode;
      }
    }
  }
}