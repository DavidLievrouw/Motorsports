using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Security {
  public class UsernamePasswordAuthenticateUserService : IAuthenticateUserService<UsernamePasswordCredentials> {
    readonly IUserDataService _userDataService;
    readonly IHashPasswordService _hashPasswordService;
    readonly IRandomHashedPasswordProvider _randomHashedPasswordProvider;
    readonly IValidator<UsernamePasswordCredentials> _usernamePasswordCredentialsValidator;

    public UsernamePasswordAuthenticateUserService(
      IUserDataService userDataService,
      IRandomHashedPasswordProvider randomHashedPasswordProvider,
      IHashPasswordService hashPasswordService,
      IValidator<UsernamePasswordCredentials> usernamePasswordCredentialsValidator) {
      _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
      _usernamePasswordCredentialsValidator = usernamePasswordCredentialsValidator ??throw new ArgumentNullException(nameof(usernamePasswordCredentialsValidator));
      _randomHashedPasswordProvider = randomHashedPasswordProvider ??throw new ArgumentNullException(nameof(randomHashedPasswordProvider));
      _hashPasswordService = hashPasswordService ?? throw new ArgumentNullException(nameof(hashPasswordService));
    }

    public async Task<AuthenticateResponse> Authenticate(UsernamePasswordCredentials credentials) {
      var validationResult = _usernamePasswordCredentialsValidator.Validate(credentials);
      if (!validationResult.IsValid)
        return new AuthenticationFailure<UsernamePasswordAuthenticateFailureReason>(validationResult.Errors.ParseFailureReasons<UsernamePasswordAuthenticateFailureReason>());

      var user = await _userDataService.GetUserForAuthentication(credentials.Username);

      // if the user is not found, use a random password to verify against and still do the work. This protects against timing attacks
      var password = user?.Password ?? _randomHashedPasswordProvider.RandomHashedPassword;

      // Verify against the stored password by performing the same hash operation on the entered password
      var hashedPassword = _hashPasswordService.Hash(
        credentials.Password,
        password.Salt,
        password.Prf,
        password.Iterations);
      if (!password.Equals(hashedPassword) || user == null) return new AuthenticationFailure<UsernamePasswordAuthenticateFailureReason>(UsernamePasswordAuthenticateFailureReason.InvalidCredentials);

      var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(user.AsClaims()));

      return new AuthenticationSuccess(claimsPrincipal);
    }
  }
}