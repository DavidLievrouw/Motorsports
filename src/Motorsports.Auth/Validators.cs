using FluentValidation;
using Motorsports.Infrastructure.Auth;

namespace Motorsports.Auth;

internal sealed record LoginForm(string? Username, string? Password, string? ReturnUrl);

internal sealed class LoginFormValidator : AbstractValidator<LoginForm>
{
    public const int MaxUsernameLength = 255;
    public const int MaxPasswordLength = 256;

    public LoginFormValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(MaxUsernameLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(MaxPasswordLength);
    }
}

internal sealed class ChangePasswordModel
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }

    public string? ReturnUrl { get; set; }
}

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordModel>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Enter your current password.")
            .MaximumLength(LoginFormValidator.MaxPasswordLength);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Enter a new password.")
            .MinimumLength(AuthExtensions.MinimumPasswordLength)
            .WithMessage($"The new password needs at least {AuthExtensions.MinimumPasswordLength} characters.")
            .MaximumLength(LoginFormValidator.MaxPasswordLength)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must differ from the current one.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage("The passwords do not match.");
    }
}
