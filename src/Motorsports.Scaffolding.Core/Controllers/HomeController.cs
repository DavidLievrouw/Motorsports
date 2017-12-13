using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class HomeController : Controller {
    public IActionResult Index() {
      return View();
    }

    public IActionResult Error() {
      return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
  }
}