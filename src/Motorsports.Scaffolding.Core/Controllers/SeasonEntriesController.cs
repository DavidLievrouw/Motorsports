using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class SeasonEntriesController : Controller {
    readonly MotorsportsContext _context;
    readonly ISeasonService _seasonService;
    readonly ISeasonEntryService _seasonEntryService;

    public SeasonEntriesController(
      MotorsportsContext context,
      ISeasonService seasonService,
      ISeasonEntryService seasonEntryService) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
      _seasonEntryService = seasonEntryService ?? throw new ArgumentNullException(nameof(seasonEntryService));
    }

    // GET: /Season/SeasonEntries/5
    public async Task<IActionResult> Index(int? id) {
      if (id == null) return NotFound();

      var season = await _seasonService.LoadDataRecord(id.Value);
      var entriesForSeason = await _seasonEntryService.LoadSeasonEntryList(id.Value);
      if (entriesForSeason == null) return NotFound();

      var displayModel = new SeasonEntryIndexDisplayModel {
        Season = season,
        SeasonEntries = entriesForSeason
      };
      return View(displayModel);
    }

    // GET: SeasonEntries/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var seasonEntry = await _context.SeasonEntry
        .Include(s => s.RelatedSeason)
        .Include(s => s.RelatedTeam)
        .SingleOrDefaultAsync(m => m.Team == id);
      if (seasonEntry == null) return NotFound();

      return View(seasonEntry);
    }

    // GET: SeasonEntries/Create
    public IActionResult Create() {
      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport");
      ViewData["Team"] = new SelectList(_context.Team, "Id", "Country");
      return View(new SeasonEntry());
    }

    // POST: SeasonEntries/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Season,Team,Name")] SeasonEntry seasonEntry) {
      if (ModelState.IsValid) {
        _context.Add(seasonEntry);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", seasonEntry.Season);
      ViewData["Team"] = new SelectList(_context.Team, "Id", "Country", seasonEntry.Team);
      return View(seasonEntry);
    }

    // GET: SeasonEntries/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var seasonEntry = await _context.SeasonEntry.SingleOrDefaultAsync(m => m.Team == id);
      if (seasonEntry == null) return NotFound();
      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", seasonEntry.Season);
      ViewData["Team"] = new SelectList(_context.Team, "Id", "Country", seasonEntry.Team);
      return View(seasonEntry);
    }

    // POST: SeasonEntries/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Season,Team,Name")] SeasonEntry seasonEntry) {
      if (id != seasonEntry.Team) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(seasonEntry);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!SeasonEntryExists(seasonEntry.Team)) return NotFound();
          else throw;
        }

        return RedirectToAction(nameof(Index));
      }

      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", seasonEntry.Season);
      ViewData["Team"] = new SelectList(_context.Team, "Id", "Country", seasonEntry.Team);
      return View(seasonEntry);
    }

    // GET: SeasonEntries/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var seasonEntry = await _context.SeasonEntry
        .Include(s => s.RelatedSeason)
        .Include(s => s.RelatedTeam)
        .SingleOrDefaultAsync(m => m.Team == id);
      if (seasonEntry == null) return NotFound();

      return View(seasonEntry);
    }

    // POST: SeasonEntries/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var seasonEntry = await _context.SeasonEntry.SingleOrDefaultAsync(m => m.Team == id);
      _context.SeasonEntry.Remove(seasonEntry);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    bool SeasonEntryExists(int id) {
      return _context.SeasonEntry.Any(e => e.Team == id);
    }
  }
}