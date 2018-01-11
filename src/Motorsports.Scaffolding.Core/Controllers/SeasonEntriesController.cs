using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Models.Validators;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
  public class SeasonEntriesController : Controller {
    readonly ISeasonService _seasonService;
    readonly ISeasonEntryService _seasonEntryService;
    readonly IModelStatePopulator<SeasonEntry, SeasonEntry.SeasonEntryKey> _seasonEntryModelStatePopulator;

    public SeasonEntriesController(
      ISeasonService seasonService,
      ISeasonEntryService seasonEntryService,
      IModelStatePopulator<SeasonEntry, SeasonEntry.SeasonEntryKey> seasonEntryModelStatePopulator) {
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
      _seasonEntryService = seasonEntryService ?? throw new ArgumentNullException(nameof(seasonEntryService));
      _seasonEntryModelStatePopulator = seasonEntryModelStatePopulator ?? throw new ArgumentNullException(nameof(seasonEntryModelStatePopulator));
    }

    // GET: SeasonEntries/5
    public async Task<IActionResult> Index(int? id) {
      if (id == null) return NotFound();

      var season = await _seasonService.LoadDataRecord(id.Value);
      var entriesForSeason = await _seasonEntryService.LoadSeasonEntryList(id.Value);
      if (entriesForSeason == null) return NotFound();

      var displayModel = new SeasonEntryIndexDisplayModel {
        Season = season,
        SeasonEntries = entriesForSeason
      };
      return View(displayModel);
    }

    // GET: SeasonEntries/Details/5
    public async Task<IActionResult> Details(int seasonId, int teamId) {
      var displayModel = await _seasonEntryService.LoadDisplayModel(seasonId, teamId);
      if (displayModel == null) return NotFound();
      return View(displayModel);
    }

    // GET: SeasonEntries/Create/5
    public async Task<IActionResult> Create(int? id) {
      if (id == null) return NotFound();
      return View(await _seasonEntryService.GetNew(id.Value));
    }

    // POST: SeasonEntries/Create/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? id, [Bind("Season,Team,Name")] SeasonEntry seasonEntry) {
      if (id == null) return NotFound();

      await _seasonEntryModelStatePopulator.ValidateAndPopulateForCreate(ModelState, seasonEntry);
      if (ModelState.IsValid) {
        await _seasonEntryService.PersistSeasonEntry(seasonEntry);
        return RedirectToAction(nameof(Index), new { id = id.Value});
      }

      var seasonEntryDisplayModel = await _seasonEntryService.GetNew(id.Value);
      if (seasonEntryDisplayModel == null) return NotFound();
      return View(seasonEntryDisplayModel);
    }

    // GET: SeasonEntries/Edit/5
    public async Task<IActionResult> Edit(int seasonId, int teamId) {
      var displayModel = await _seasonEntryService.LoadDisplayModel(seasonId, teamId);
      if (displayModel == null) return NotFound();
      return View(displayModel);
    }

    // POST: SeasonEntries/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Season,Team,Name")] [ModelBinder(typeof(SeasonEntryEditModel.SeasonEntryEditModelBinder))] SeasonEntryEditModel seasonEntry) {
      if (id == null) return NotFound();
      if (id != seasonEntry.Season) return NotFound();

      var seasonEntryForValidation = new SeasonEntry {
        Season = seasonEntry.Season,
        Team = seasonEntry.Team,
        Name = seasonEntry.Name
      };
      await _seasonEntryModelStatePopulator.ValidateAndPopulateForUpdate(
        ModelState, 
        new SeasonEntry.SeasonEntryKey(seasonEntry.Season, seasonEntry.Team), 
        seasonEntryForValidation);
      
      if (ModelState.IsValid) {
        await _seasonEntryService.UpdateSeasonEntry(seasonEntry);
        return RedirectToAction(nameof(Index), new { id = seasonEntry.Season });
      }
      
      return View(await _seasonEntryService.LoadDisplayModel(seasonEntry.Season, seasonEntry.Team));
    }

    // GET: SeasonEntries/Delete/5
    public async Task<IActionResult> Delete(int seasonId, int teamId) {
      var seasonEntryDisplayModel = await _seasonEntryService.LoadDisplayModel(seasonId, teamId);
      if (seasonEntryDisplayModel == null) return NotFound();
      return View(seasonEntryDisplayModel);
    }

    // POST: SeasonEntries/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int seasonId, int teamId) {
      var seasonEntryDisplayModel = await _seasonEntryService.LoadDisplayModel(seasonId, teamId);
      if (seasonEntryDisplayModel == null) return NotFound();

      await _seasonEntryModelStatePopulator.ValidateAndPopulateForDelete(ModelState, seasonEntryDisplayModel.DataModel);
      if (ModelState.IsValid) {
        await _seasonEntryService.DeleteSeasonEntry(seasonId, teamId);
        return RedirectToAction(nameof(Index), new { id = seasonEntryDisplayModel.Season });
      }

      return View(seasonEntryDisplayModel);
    }
  }
}