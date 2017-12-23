using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonService {
    Task UpdateSeason(SeasonEditModel season);
  }

  public class SeasonService : ISeasonService {
    readonly MotorsportsContext _context;

    public SeasonService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
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
  }
}