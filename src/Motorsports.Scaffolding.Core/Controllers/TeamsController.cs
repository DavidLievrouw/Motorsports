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
  public class TeamsController : Controller {
    readonly MotorsportsContext _context;
    readonly IModelStatePopulator<Team> _teamModelStatePopulator;

    public TeamsController(MotorsportsContext context, IModelStatePopulator<Team> teamModelStatePopulator) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _teamModelStatePopulator = teamModelStatePopulator ?? throw new ArgumentNullException(nameof(teamModelStatePopulator));
    }

    // GET: Teams
    public async Task<IActionResult> Index() {
      var motorsportsContext = _context.Team.Include(t => t.RelatedCountry).Include(t => t.RelatedSport);
      return View(await motorsportsContext.ToListAsync());
    }

    // GET: Teams/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var team = await _context.Team
        .Include(t => t.RelatedCountry)
        .Include(t => t.RelatedSport)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (team == null) return NotFound();

      return View(team);
    }

    // GET: Teams/Create
    public IActionResult Create() {
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "Iso");
      ViewData["Sport"] = new SelectList(_context.Sport.OrderBy(_ => _.Name), "Name", "Name");
      return View(new Team());
    }

    // POST: Teams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Sport,Country")] Team team) {
      await _teamModelStatePopulator.ValidateAndPopulateForCreate(ModelState, team);
      if (ModelState.IsValid) {
        _context.Add(team);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "Iso", team.Country);
      ViewData["Sport"] = new SelectList(_context.Sport.OrderBy(_ => _.Name), "Name", "Name", team.Sport);
      return View(team);
    }

    // GET: Teams/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var team = await _context.Team.SingleOrDefaultAsync(m => m.Id == id);
      if (team == null) return NotFound();
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "Iso", team.Country);
      ViewData["Sport"] = new SelectList(_context.Sport.OrderBy(_ => _.Name), "Name", "Name", team.Sport);
      return View(team);
    }

    // POST: Teams/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Sport,Country")] Team team) {
      if (id != team.Id) return NotFound();

      await _teamModelStatePopulator.ValidateAndPopulateForUpdate(ModelState, team);
      if (ModelState.IsValid) {
        try {
          _context.Update(team);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!TeamExists(team.Id)) return NotFound();
          else throw;
        }
        return RedirectToAction(nameof(Index));
      }
      ViewData["Country"] = new SelectList(_context.Country.OrderBy(_ => _.NiceName), "Iso", "Iso", team.Country);
      ViewData["Sport"] = new SelectList(_context.Sport.OrderBy(_ => _.Name), "Name", "Name", team.Sport);
      return View(team);
    }
    
    bool TeamExists(int id) {
      return _context.Team.Any(e => e.Id == id);
    }
  }
}