using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
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

    public IActionResult Error() {
      return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
  }
}