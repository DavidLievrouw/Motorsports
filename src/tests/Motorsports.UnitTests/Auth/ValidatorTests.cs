using Motorsports.Auth;
using Shouldly;

namespace Motorsports.UnitTests.Auth;

public class ValidatorTests
{
    [Fact]
    public void Login_requires_username_and_password_within_limits()
    {
        var validator = new LoginFormValidator();

        validator.Validate(new LoginForm("me", "secret", null)).IsValid.ShouldBeTrue();
        validator.Validate(new LoginForm("", "secret", null)).IsValid.ShouldBeFalse();
        validator.Validate(new LoginForm("me", null, null)).IsValid.ShouldBeFalse();
        validator.Validate(new LoginForm(new string('a', 256), "secret", null)).IsValid.ShouldBeFalse();
        validator.Validate(new LoginForm("me", new string('a', 257), null)).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("old", "newpassword", "newpassword", true)]
    [InlineData("old", "short", "short", false)]
    [InlineData("old", "newpassword", "different1", false)]
    [InlineData("samepassword", "samepassword", "samepassword", false)]
    [InlineData("", "newpassword", "newpassword", false)]
    public void Change_password_rules(string current, string next, string confirm, bool valid)
    {
        var model = new ChangePasswordModel
        {
            CurrentPassword = current,
            NewPassword = next,
            ConfirmPassword = confirm,
        };

        new ChangePasswordValidator().Validate(model).IsValid.ShouldBe(valid);
    }
}
