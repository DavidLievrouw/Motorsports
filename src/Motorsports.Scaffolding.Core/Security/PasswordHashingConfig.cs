using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Motorsports.Scaffolding.Core.Security {
  public class PasswordHashingConfig {
    public int SaltSizeInBytes { get; set; } = 16;
    public KeyDerivationPrf KeyDerivationPrf { get; set; } = KeyDerivationPrf.HMACSHA512;
    public int Iterations { get; set; } = 10000;
    public int HashSizeInBytes { get; set; } = 32;
  }
}