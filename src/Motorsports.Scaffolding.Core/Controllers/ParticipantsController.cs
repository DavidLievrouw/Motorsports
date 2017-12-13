using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class ParticipantsController : Controller {
    readonly MotorsportsContext _context;

    public ParticipantsController(MotorsportsContext context) {
      _context = context;
    }

    // GET: Participants
    public async Task<IActionResult> Index() {
      var motorsportsContext = _context.Participant.Include(p => p.CountryNavigation);
      return View(model: await motorsportsContext.ToListAsync());
    }

    // GET: Participants/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var participant = await _context.Participant
        .Include(p => p.CountryNavigation)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (participant == null) return NotFound();

      return View(participant);
    }

    // GET: Participants/Create
    public IActionResult Create() {
      ViewData["Country"] = new SelectList(_context.Country, "Iso", "NiceName");
      return View();
    }

    // POST: Participants/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,FirstName,LastName,Country")] Participant participant) {
      if (ModelState.IsValid) {
        _context.Add(participant);
        await _context.SaveChangesAsync();
        return RedirectToAction(actionName: nameof(Index));
      }
      ViewData["Country"] = new SelectList(items: _context.Country.OrderBy(_ => _.NiceName), dataValueField: "Iso", dataTextField: "NiceName", selectedValue: participant.Country);
      return View(participant);
    }

    // GET: Participants/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var participant = await _context.Participant.SingleOrDefaultAsync(m => m.Id == id);
      if (participant == null) return NotFound();
      ViewData["Country"] = new SelectList(items: _context.Country.OrderBy(_ => _.NiceName), dataValueField: "Iso", dataTextField: "NiceName", selectedValue: participant.Country);
      return View(participant);
    }

    // POST: Participants/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,FirstName,LastName,Country")] Participant participant) {
      if (id != participant.Id) return NotFound();

      if (ModelState.IsValid) {
        try {
          _context.Update(participant);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!ParticipantExists(participant.Id)) return NotFound();
          else throw;
        }
        return RedirectToAction(actionName: nameof(Index));
      }
      ViewData["Country"] = new SelectList(items: _context.Country.OrderBy(_ => _.NiceName), dataValueField: "Iso", dataTextField: "NiceName", selectedValue: participant.Country);
      return View(participant);
    }

    // GET: Participants/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var participant = await _context.Participant
        .Include(p => p.CountryNavigation)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (participant == null) return NotFound();

      return View(participant);
    }

    // POST: Participants/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var participant = await _context.Participant.SingleOrDefaultAsync(m => m.Id == id);
      _context.Participant.Remove(participant);
      await _context.SaveChangesAsync();
      return RedirectToAction(actionName: nameof(Index));
    }

    bool ParticipantExists(int id) {
      return _context.Participant.Any(e => e.Id == id);
    }
  }
}