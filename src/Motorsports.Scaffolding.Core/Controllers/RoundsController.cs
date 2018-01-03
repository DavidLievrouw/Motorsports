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
  public class RoundsController : Controller {
    readonly IRoundService _roundService;
    readonly ISeasonService _seasonService;
    readonly IModelStatePopulator<Round> _roundModelStatePopulator;

    public RoundsController(
      IRoundService roundService, 
      ISeasonService seasonService,
      IModelStatePopulator<Round> roundModelStatePopulator) {
      _roundService = roundService ?? throw new ArgumentNullException(nameof(roundService));
      _seasonService = seasonService ?? throw new ArgumentNullException(nameof(seasonService));
      _roundModelStatePopulator = roundModelStatePopulator ?? throw new ArgumentNullException(nameof(roundModelStatePopulator));
    }

    // GET: /Rounds/Season/5
    public async Task<IActionResult> Index(int? id) {
      if (id == null) return NotFound();

      var season = await _seasonService.LoadDataRecord(id.Value);
      var roundsForSeason = await _roundService.LoadRoundList(id.Value);
      if (roundsForSeason == null) return NotFound();

      var displayModel = new RoundsIndexDisplayModel {
        Season = season,
        Rounds = roundsForSeason
      };
      return View(displayModel);
    }

    // GET: Rounds/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) return NotFound();

      var round = await _roundService.LoadDisplayModel(id.Value);
      if (round == null) return NotFound();

      return View(round);
    }

    // GET: Rounds/Create/5
    public async Task<IActionResult> Create(int? id) {
      if (id == null) return NotFound();
      return View(await _roundService.GetNew(id.Value));
    }

    // POST: Rounds/Create/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? id,[Bind("Season,Date,Number,Name,Venue,Status")] Round round) {
      if (id == null) return NotFound();
      
      await _roundModelStatePopulator.ValidateAndPopulateForCreate(ModelState, round);
      if (ModelState.IsValid) {
        await _roundService.PersistRound(round);
        return RedirectToAction(nameof(Index), new { id = id.Value});
      }

      var roundDisplayModel = await _roundService.CreateForRound(round);
      if (roundDisplayModel == null) return NotFound();
      return View(roundDisplayModel);
    }

    // GET: Rounds/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();
      var round = await _roundService.LoadDisplayModel(id.Value);
      if (round == null) return NotFound();
      return View(round);
    }

    // POST: Rounds/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,[Bind("Id,Name,Date,Number,Venue,Rain,Rating,Status,WinningTeamId,WinningParticipantIds[]")] [ModelBinder(typeof(RoundEditModel.RoundEditModelBinder))] RoundEditModel round) {
      if (id != round.Id) return NotFound();

      var roundForValidation = await _roundService.LoadDataRecord(id);
      roundForValidation.Season = round.Season;
      roundForValidation.Name = round.Name;
      roundForValidation.Date = round.Date;
      roundForValidation.Number = round.Number;
      roundForValidation.Venue = round.Venue;
      await _roundModelStatePopulator.ValidateAndPopulateForUpdate(ModelState, roundForValidation);
      
      if (ModelState.IsValid) {
        await _roundService.UpdateRound(round);
        var roundDisplayModel = await _roundService.LoadDisplayModel(id);
        if (roundDisplayModel == null) return NotFound();
        return RedirectToAction(nameof(Index), new { id = roundDisplayModel.Season });
      }

      return View(await _roundService.LoadDisplayModel(id));
    }

    // GET: Rounds/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) return NotFound();

      var round = await _roundService.LoadDisplayModel(id.Value);
      if (round == null) return NotFound();

      return View(round);
    }

    // POST: Rounds/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var round = await _roundService.LoadDisplayModel(id);
      if (round == null) return NotFound();
      await _roundService.DeleteRound(id);
      return RedirectToAction(nameof(Index), new { id = round.Season });
    }
  }
}