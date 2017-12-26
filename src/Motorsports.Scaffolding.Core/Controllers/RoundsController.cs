using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.EditModels;
using Motorsports.Scaffolding.Core.Services;

namespace Motorsports.Scaffolding.Core.Controllers {
  public class RoundsController : Controller {
    readonly IRoundService _roundService;

    public RoundsController(IRoundService roundService) {
      _roundService = roundService ?? throw new ArgumentNullException(nameof(roundService));
    }

    // GET: /Rounds/Season/5
    public async Task<IActionResult> Index(int? id) {
      if (id == null) return NotFound();
      
      var roundsForSeason = await _roundService.LoadRoundList(id.Value);
      if (roundsForSeason == null) return NotFound();

      return View(roundsForSeason);
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
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? id,[Bind("Date,Number,Name,Venue")] Round round) {
      if (id == null) return NotFound();

      if (ModelState.IsValid) {
        round.Season = id.Value;
        await _roundService.CreateRound(round);
        return RedirectToAction(nameof(Index), new { id = id.Value});
      }

      return View(round);
    }

    // GET: Rounds/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) return NotFound();
      var round = await _roundService.LoadDisplayModel(id.Value);
      if (round == null) return NotFound();
      return View(round);
    }

    // POST: Rounds/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,[Bind("Id,Name,Date,Number,Venue,Rain,Rating,Status,WinningTeamId,WinningParticipantIds[]")] [ModelBinder(typeof(RoundEditModel.RoundEditModelBinder))] RoundEditModel round) {
      if (id != round.Id) return NotFound();
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