using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Models.Validators;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class SeasonsController : Controller {
    readonly ISeasonService _seasonService;
    readonly IModelStatePopulator<Season> _seasonModelStatePopulator;

    public SeasonsController(ISeasonService seasonService, IModelStatePopulator<Season> seasonModelStatePopulator) {
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
      _seasonModelStatePopulator = seasonModelStatePopulator ?? throw new ArgumentNullException(nameof(seasonModelStatePopulator));
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Sport,Label")] Season season) {
      await _seasonModelStatePopulator.ValidateAndPopulateForCreate(ModelState, season);
      if (ModelState.IsValid) {
        await _seasonService.PersistSeason(season);
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Sport,WinningTeamId,WinningParticipantIds[]")] [ModelBinder(typeof(SeasonEditModel.SeasonEditModelBinder))] SeasonEditModel season) {
      if (id != season.Id) return NotFound();

      var seasonForValidation = await _seasonService.LoadDataRecord(id);
      seasonForValidation.Label = season.Label;
      await _seasonModelStatePopulator.ValidateAndPopulateForUpdate(ModelState, seasonForValidation);

      if (ModelState.IsValid) {
        await _seasonService.UpdateSeason(season);
        return RedirectToAction(nameof(Index));
      }

      return View(await _seasonService.LoadDisplayModel(id));
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