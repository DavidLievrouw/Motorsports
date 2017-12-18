using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class SeasonsController : Controller {
    readonly MotorsportsContext _context;

    public SeasonsController(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // GET: Seasons
    public async Task<IActionResult> Index() {
      var motorsportsContext = _context.Season.Include(s => s.RelatedSport);
      var indexItems = (await motorsportsContext.ToListAsync()).Select(s => new SeasonDisplayModel(s));
      return View(indexItems);
    }

    // GET: Seasons/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var season = await _context.Season
        .Include(s => s.RelatedSport)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (season == null) return NotFound();

      return View(new SeasonDisplayModel(season));
    }

    // GET: Seasons/Create
    public IActionResult Create() {
      return View(new SeasonEditModel(
        new Season(),
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    // POST: Seasons/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Sport,Label")] Season season) {
      if (ModelState.IsValid) {
        _context.Add(season);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      return View(new SeasonEditModel(
        new Season(),
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    // GET: Seasons/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var season = await _context.Season.SingleOrDefaultAsync(m => m.Id == id);
      if (season == null) return NotFound();
      return View(new SeasonEditModel(
        season,
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.Where(team => team.Sport == season.Sport).OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    // POST: Seasons/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Sport,Label")] Season season) {
      if (id != season.Id) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(season);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!SeasonExists(season.Id)) return NotFound();
          else throw;
        }
        return RedirectToAction(nameof(Index));
      }
      return View(new SeasonEditModel(
        season,
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.Where(team => team.Sport == season.Sport).OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    // GET: Seasons/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var season = await _context.Season
        .Include(s => s.RelatedSport)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (season == null) return NotFound();

      return View(new SeasonDisplayModel(season));
    }

    // POST: Seasons/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var season = await _context.Season.SingleOrDefaultAsync(m => m.Id == id);
      _context.Season.Remove(season);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    bool SeasonExists(int id) {
      return _context.Season.Any(e => e.Id == id);
    }
  }
}