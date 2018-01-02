using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Security;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Route("")]
  [AllowAnonymous]
  public class LoginController : Controller {
    readonly IAuthenticateUserService<UsernamePasswordCredentials> _authUserService;

    public LoginController(IAuthenticateUserService<UsernamePasswordCredentials> authUserService) {
      _authUserService = authUserService ?? throw new ArgumentNullException(nameof(authUserService));
    }

    [HttpGet("login")]
    public IActionResult Index() {
      return View(new LoginDisplayModel());
    }
    
    [HttpGet("accessdenied")]
    public IActionResult AccessDenied() {
      return View("Index", new LoginDisplayModel { AccessDenied = true });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string returnUrl, [Bind("Username,Password")] LoginEditModel form) {
      var authenticationResult = await _authUserService.Authenticate(
        new UsernamePasswordCredentials {
          Username = form?.Username,
          Password = new PlainTextPassword(form?.Password)
        });

      if (authenticationResult is AuthenticationFailure<UsernamePasswordAuthenticateFailureReason> failure) {
        var loginViewModel = new LoginDisplayModel {ReturnUrl = returnUrl, Form = form};
        loginViewModel.AddFailureToModelState(failure, ModelState);
        return View("Index", loginViewModel);
      }

      if (authenticationResult is AuthenticationSuccess authenticationSucces) {
        var principal = authenticationSucces.Principal;
        await HttpContext.SignInAsync(principal);
      }

      return returnUrl != null
        ? Redirect(returnUrl) as IActionResult
        : RedirectToAction("Index", "Home");
    }

    [HttpGet("logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout() {
      await HttpContext.SignOutAsync();
      return RedirectToAction("Login");
    }
  }
}