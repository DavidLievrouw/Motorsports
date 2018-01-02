using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class HomeController : Controller {
    readonly INextUpService _nextUpService;

    public HomeController(INextUpService nextUpService) {
      _nextUpService = nextUpService ?? throw new ArgumentNullException(nameof(nextUpService));
    }

    public async Task<IActionResult> Index() {
      var nextUps = await _nextUpService.GetRoundsNextUp();
      var displayModel = new HomeDisplayModel(nextUps);
      return View(displayModel);
    }
  }
}