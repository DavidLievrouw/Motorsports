using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class RoundsController : Controller {
    readonly MotorsportsContext _context;

    public RoundsController(MotorsportsContext context) {
      _context = context;
    }

    // GET: Rounds
    public async Task<IActionResult> Index() {
      var motorsportsContext = _context.Round.Include(r => r.RelatedSeason).Include(r => r.RelatedVenue);
      return View(await motorsportsContext.ToListAsync());
    }

    // GET: Rounds/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var round = await _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedVenue)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (round == null) return NotFound();

      return View(round);
    }

    // GET: Rounds/Create
    public IActionResult Create() {
      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport");
      ViewData["Venue"] = new SelectList(_context.Venue, "Name", "Name");
      return View(new Round());
    }

    // POST: Rounds/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
      [Bind("Id,Date,Number,Name,Season,Venue")]
      Round round) {
      if (ModelState.IsValid) {
        _context.Add(round);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", round.Season);
      ViewData["Venue"] = new SelectList(_context.Venue, "Name", "Name", round.Venue);
      return View(round);
    }

    // GET: Rounds/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var round = await _context.Round.SingleOrDefaultAsync(m => m.Id == id);
      if (round == null) return NotFound();
      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", round.Season);
      ViewData["Venue"] = new SelectList(_context.Venue, "Name", "Name", round.Venue);
      return View(round);
    }

    // POST: Rounds/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
      int id,
      [Bind("Id,Date,Number,Name,Season,Venue")]
      Round round) {
      if (id != round.Id) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(round);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!RoundExists(round.Id)) return NotFound();
          else throw;
        }

        return RedirectToAction(nameof(Index));
      }

      ViewData["Season"] = new SelectList(_context.Season, "Id", "Sport", round.Season);
      ViewData["Venue"] = new SelectList(_context.Venue, "Name", "Name", round.Venue);
      return View(round);
    }

    // GET: Rounds/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var round = await _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedVenue)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (round == null) return NotFound();

      return View(round);
    }

    // POST: Rounds/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var round = await _context.Round.SingleOrDefaultAsync(m => m.Id == id);
      _context.Round.Remove(round);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    bool RoundExists(int id) {
      return _context.Round.Any(e => e.Id == id);
    }
  }
}