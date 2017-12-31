using FluentValidation;

namespace Motorsports.Scaffolding.Core.Security {
  public class UsernamePasswordCredentialsValidator : AbstractValidator<UsernamePasswordCredentials> {
    public UsernamePasswordCredentialsValidator() {
      RuleFor(c => c.Username).Must(u => !string.IsNullOrWhiteSpace(u))
        .WithErrorCode(UsernamePasswordAuthenticateFailureReason.UsernameIsEmpty.ToString());
      RuleFor(c => c.Password).Must(u => !string.IsNullOrWhiteSpace(u))
        .WithErrorCode(UsernamePasswordAuthenticateFailureReason.PasswordIsEmpty.ToString());
    }
  }
}