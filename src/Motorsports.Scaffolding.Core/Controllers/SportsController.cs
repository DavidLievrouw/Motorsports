using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.Validators;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class SportsController : Controller {
    readonly MotorsportsContext _context;
    readonly ISportService _sportService;
    readonly IModelStatePopulator<Sport, string> _sportModelStatePopulator;

    public SportsController(
      MotorsportsContext context, 
      ISportService sportService,
      IModelStatePopulator<Sport, string> sportModelStatePopulator) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _sportService = sportService ?? throw new ArgumentNullException(nameof(sportService));
      _sportModelStatePopulator = sportModelStatePopulator ?? throw new ArgumentNullException(nameof(sportModelStatePopulator));
    }

    // GET: Sports
    public async Task<IActionResult> Index() {
      return View(model: await _context.Sport.ToListAsync());
    }

    // GET: Sports/Details/5
    public async Task<IActionResult> Details(string id) {
      if (id == null) return NotFound();
      
      var sport = await _context.Sport
        .SingleOrDefaultAsync(m => m.Name == id);
      if (sport == null) return NotFound();

      return View(sport);
    }

    // GET: Sports/Create
    public IActionResult Create() {
      return View(new Sport());
    }

    // POST: Sports/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,FullName")] Sport sport) {
      await _sportModelStatePopulator.ValidateAndPopulateForCreate(ModelState, sport);
      if (ModelState.IsValid) {
        _context.Add(sport);
        await _context.SaveChangesAsync();
        return RedirectToAction(actionName: nameof(Index));
      }
      return View(sport);
    }

    // GET: Sports/Edit/5
    public async Task<IActionResult> Edit(string id) {
      if (id == null) return NotFound();

      var sport = await _context.Sport.SingleOrDefaultAsync(m => m.Name == id);
      if (sport == null) return NotFound();
      return View(sport);
    }

    // POST: Sports/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Name,FullName")] Sport sport) {
      await _sportModelStatePopulator.ValidateAndPopulateForUpdate(ModelState, id, sport);
      if (ModelState.IsValid) {
        try {
          // EF does not allow updating the primary key.
          // So keep EF context manually up-to-date, and do the db work ourselves.
          _context.Update(sport);
          _context.Entry(sport).State = EntityState.Unchanged;
          await _sportService.UpdateSport(id, sport);
        } catch (DbUpdateConcurrencyException) {
          if (!SportExists(sport.Name)) return NotFound();
          else throw;
        }
        return RedirectToAction(actionName: nameof(Index));
      }
      return View(sport);
    }

    bool SportExists(string id) {
      return _context.Sport.Any(e => e.Name == id);
    }
  }
}