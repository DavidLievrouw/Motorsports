using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.Validators;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class ParticipantsController : Controller {
    readonly MotorsportsContext _context;
    readonly IModelStatePopulator<Participant, int> _participantModelStatePopulator;

    public ParticipantsController(MotorsportsContext context, IModelStatePopulator<Participant, int> participantModelStatePopulator) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _participantModelStatePopulator = participantModelStatePopulator ?? throw new ArgumentNullException(nameof(participantModelStatePopulator));
    }

    // GET: Participants
    public async Task<IActionResult> Index() {
      var participants = await _context.Participant
        .Include(p => p.RelatedCountry)
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .ThenBy(p => p.Country)
        .ToListAsync();
      return View(participants);
    }

    // GET: Participants/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var participant = await _context.Participant
        .Include(p => p.RelatedCountry)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (participant == null) return NotFound();

      return View(participant);
    }

    // GET: Participants/Create
    public IActionResult Create() {
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "NiceName");
      return View(new Participant());
    }

    // POST: Participants/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,FirstName,LastName,Country")] Participant participant) {
      await _participantModelStatePopulator.ValidateAndPopulateForCreate(ModelState, participant);
      if (ModelState.IsValid) {
        _context.Add(participant);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "NiceName", participant.Country);
      return View(participant);
    }

    // GET: Participants/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var participant = await _context.Participant.SingleOrDefaultAsync(m => m.Id == id);
      if (participant == null) return NotFound();
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "NiceName", participant.Country);
      return View(participant);
    }

    // POST: Participants/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,FirstName,LastName,Country")] Participant participant) {
      if (id != participant.Id) return NotFound();

      await _participantModelStatePopulator.ValidateAndPopulateForUpdate(ModelState, id, participant);
      if (ModelState.IsValid) {
        try {
          _context.Update(participant);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!ParticipantExists(participant.Id)) return NotFound();
          else throw;
        }
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "NiceName", participant.Country);
      return View(participant);
    }

    bool ParticipantExists(int id) {
      return _context.Participant.Any(e => e.Id == id);
    }
  }
}