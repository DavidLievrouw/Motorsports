using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class SeasonsController : Controller {
    readonly ISeasonService _seasonService;

    public SeasonsController(ISeasonService seasonService) {
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
    }

    // GET: Seasons
    public async Task<IActionResult> Index() {
      return View(await _seasonService.LoadSeasonList());
    }

    // GET: Seasons/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var season = await _seasonService.LoadDisplayModel(id.Value);
      if (season == null) return NotFound();

      return View(season);
    }

    // GET: Seasons/Create
    public async Task<IActionResult> Create() {
      return View(await _seasonService.GetNew());
    }

    // POST: Seasons/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Sport,Label")] Season season) {
      if (ModelState.IsValid) {
        await _seasonService.CreateSeason(season);
        return RedirectToAction(nameof(Index));
      }
      return View(await _seasonService.GetNew());
    }

    // GET: Seasons/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();
      
      var season = await _seasonService.LoadDisplayModel(id.Value);
      if (season == null) return NotFound();

      return View(season);
    }

    // POST: Seasons/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Sport,WinningTeamId,WinningParticipantIds[]")] [ModelBinder(typeof(SeasonEditModel.SeasonEditModelBinder))] SeasonEditModel season) {
      if (id != season.Id) return NotFound();

      if (ModelState.IsValid) {
        await _seasonService.UpdateSeason(season);
        return RedirectToAction(nameof(Index));
      }

      return View(await _seasonService.LoadDisplayModel(season.Id));
    }

    // GET: Seasons/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var season = await _seasonService.LoadDisplayModel(id.Value);
      if (season == null) return NotFound();

      return View(season);
    }

    // POST: Seasons/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      await _seasonService.DeleteSeason(id);
      return RedirectToAction(nameof(Index));
    }
  }
}