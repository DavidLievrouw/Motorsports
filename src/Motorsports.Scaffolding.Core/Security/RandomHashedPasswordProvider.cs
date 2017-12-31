using System;

namespace Motorsports.Scaffolding.Core.Security {
  public interface IRandomHashedPasswordProvider {
    HashedPassword RandomHashedPassword { get; }
  }

  public class RandomHashedPasswordProvider : IRandomHashedPasswordProvider {
    public RandomHashedPasswordProvider(IHashPasswordService hashPasswordService) {
      if (hashPasswordService == null) throw new ArgumentNullException(nameof(hashPasswordService));
      RandomHashedPassword = hashPasswordService.Hash(new PlainTextPassword(Guid.NewGuid().ToString()));
    }

    public HashedPassword RandomHashedPassword { get; }
  }
}