using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonService {
    Task<SeasonDisplayModel> GetNew();
    Task<List<SeasonDisplayModel>> LoadSeasonList();
    Task<SeasonDisplayModel> LoadDisplayModel(int seasonId);
    Task UpdateSeason(SeasonEditModel season);
    Task CreateSeason(Season season);
    Task DeleteSeason(int seasonId);
  }

  public class SeasonService : ISeasonService {
    readonly MotorsportsContext _context;

    public SeasonService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SeasonDisplayModel> GetNew() {
      return Task.FromResult(
        new SeasonDisplayModel(
          new Season(),
          _context.Sport.OrderBy(sport => sport.Name),
          _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
          _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    public Task<List<SeasonDisplayModel>> LoadSeasonList() {
      return _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedSeasonResult)
        .ThenInclude(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .Select(s => new SeasonDisplayModel(s, null, null, null))
        .ToListAsync();
    }

    public async Task<SeasonDisplayModel> LoadDisplayModel(int seasonId) {
      var seasonDataModel = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedSeasonResult)
        .ThenInclude(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .SingleOrDefaultAsync(m => m.Id == seasonId);

      return new SeasonDisplayModel(
        seasonDataModel,
        _context.Sport.OrderBy(sport => sport.Name),
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName));
    }

    public async Task UpdateSeason(SeasonEditModel season) {
      if (season == null) throw new ArgumentNullException(nameof(season));

      // Find record to update
      var seasonToUpdate = _context.Season
        .Include(s => s.RelatedSeasonResult)
        .Include(s => s.RelatedSeasonWinners)
        .Single(s => s.Id == season.Id);

      // Update winning team
      if (seasonToUpdate.RelatedSeasonResult == null && season.WinningTeamId.HasValue ||
          seasonToUpdate.RelatedSeasonResult?.WinningTeam != season.WinningTeamId) {
        _context.SeasonResult.RemoveRange(_context.SeasonResult.Where(sr => sr.Season == season.Id));
        if (season.WinningTeamId.HasValue) {
          seasonToUpdate.RelatedSeasonResult = new SeasonResult {
            Season = season.Id,
            WinningTeam = season.WinningTeamId.Value
          };
        }
      }

      // Update winners
      _context.SeasonWinner.RemoveRange(_context.SeasonWinner.Where(sw => sw.Season == season.Id));
      foreach (var winningParticipantId in season.WinningParticipantIds) {
        _context.SeasonWinner.Add(
          new SeasonWinner {
            Season = season.Id,
            Participant = winningParticipantId
          });
      }

      // Update label
      seasonToUpdate.Label = season.Label;

      // Commit
      _context.Update(seasonToUpdate);
      await _context.SaveChangesAsync();
    }

    public async Task CreateSeason(Season season) {
      _context.Add(season);
      await _context.SaveChangesAsync();
    }

    public async Task DeleteSeason(int seasonId) {
      var season = await _context.Season.SingleOrDefaultAsync(m => m.Id == seasonId);
      _context.Season.Remove(season);
      await _context.SaveChangesAsync();
    }
  }
}