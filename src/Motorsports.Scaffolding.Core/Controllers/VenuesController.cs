using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class VenuesController : Controller {
    readonly MotorsportsContext _context;

    public VenuesController(MotorsportsContext context) {
      _context = context;
    }

    // GET: Venues
    public async Task<IActionResult> Index() {
      var motorsportsContext = _context.Venue.Include(v => v.RelatedCountry);
      return View(await motorsportsContext.ToListAsync());
    }

    // GET: Venues/Details/5
    public async Task<IActionResult> Details(string id) {
      if (id == null) return NotFound();

      var venue = await _context.Venue
        .Include(v => v.RelatedCountry)
        .SingleOrDefaultAsync(m => m.Name == id);
      if (venue == null) return NotFound();

      return View(venue);
    }

    // GET: Venues/Create
    public IActionResult Create() {
      ViewData["Country"] = new SelectList(_context.Country, "Iso", "Iso");
      return View(new Venue());
    }

    // POST: Venues/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Country")] Venue venue) {
      if (ModelState.IsValid) {
        _context.Add(venue);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country, "Iso", "Iso", venue.Country);
      return View(venue);
    }

    // GET: Venues/Edit/5
    public async Task<IActionResult> Edit(string id) {
      if (id == null) return NotFound();

      var venue = await _context.Venue.SingleOrDefaultAsync(m => m.Name == id);
      if (venue == null) return NotFound();
      ViewData["Country"] = new SelectList(_context.Country, "Iso", "Iso", venue.Country);
      return View(venue);
    }

    // POST: Venues/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Name,Country")] Venue venue) {
      if (id != venue.Name) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(venue);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!VenueExists(venue.Name)) return NotFound();
          else throw;
        }
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country, "Iso", "Iso", venue.Country);
      return View(venue);
    }

    bool VenueExists(string id) {
      return _context.Venue.Any(e => e.Name == id);
    }
  }
}