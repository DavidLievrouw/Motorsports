using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class SeasonsController : Controller {
    readonly ISeasonService _seasonService;
    readonly MotorsportsContext _context;

    public SeasonsController(MotorsportsContext context, ISeasonService seasonService) {
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
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
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (season == null) return NotFound();

      return View(new SeasonDisplayModel(season));
    }

    // GET: Seasons/Create
    public IActionResult Create() {
      return View(new SeasonDisplayModel(
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
      return View(new SeasonDisplayModel(
        new Season(),
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    // GET: Seasons/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();

      var season = await _context.Season
        .Include(s => s.RelatedSeasonResult)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .SingleOrDefaultAsync(m => m.Id == id);
      if (season == null) return NotFound();
      return View(new SeasonDisplayModel(
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,Sport,WinningTeamId,WinningParticipantIds[]"), ModelBinder(typeof(SeasonEditModel.SeasonEditModelBinder))] SeasonEditModel season) {
      if (id != season.Id) return NotFound();

      if (ModelState.IsValid) {
        await _seasonService.UpdateSeason(season);
        return RedirectToAction(nameof(Index));
      }
      var seasonDataModel = _context.Season.Single(s => s.Id == season.Id);
      return View(new SeasonDisplayModel(
        seasonDataModel,
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.Where(team => team.Sport == seasonDataModel.Sport).OrderBy(team => team.Sport).ThenBy(team => team.Name),
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
  }
}