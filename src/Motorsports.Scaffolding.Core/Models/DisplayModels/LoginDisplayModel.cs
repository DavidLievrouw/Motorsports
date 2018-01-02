using Microsoft.AspNetCore.Mvc.ModelBinding;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Security;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class LoginDisplayModel {
    public string ReturnUrl { get; set; }
    public bool AccessDenied { get; set; }
    public LoginEditModel Form { get; set; }

    public void AddFailureToModelState(
      AuthenticationFailure<UsernamePasswordAuthenticateFailureReason> failure,
      ModelStateDictionary modelState) {
      foreach (var reason in failure.FailureReasons) {
        switch (reason) {
          case UsernamePasswordAuthenticateFailureReason.UsernameIsEmpty:
            modelState.AddModelError<LoginDisplayModel>(m => m.Form.Username, "The user name is empty.");
            break;
          case UsernamePasswordAuthenticateFailureReason.PasswordIsEmpty:
            modelState.AddModelError<LoginDisplayModel>(m => m.Form.Password, "The password is empty.");
            break;
          case UsernamePasswordAuthenticateFailureReason.InvalidCredentials:
            modelState.AddModelError<LoginDisplayModel>(m => m.Form.Password, "The specified credentials are invalid.");
            break;
        }
      }
    }
  }
}