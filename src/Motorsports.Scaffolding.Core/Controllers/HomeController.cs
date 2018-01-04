using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class HomeController : Controller {
    readonly IHomeService _homeService;

    public HomeController(IHomeService homeService) {
      _homeService = homeService ?? throw new ArgumentNullException(nameof(homeService));
    }

    public async Task<IActionResult> Index() {
      return View(await _homeService.GetHomeDisplayModel());
    }
  }
}