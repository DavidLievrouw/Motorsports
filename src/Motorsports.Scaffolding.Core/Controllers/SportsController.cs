using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class SportsController : Controller {
    readonly MotorsportsContext _context;

    public SportsController(MotorsportsContext context) {
      _context = context;
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
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,FullName")] Sport sport) {
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
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Name,FullName")] Sport sport) {
      if (id != sport.Name) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(sport);
          await _context.SaveChangesAsync();
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