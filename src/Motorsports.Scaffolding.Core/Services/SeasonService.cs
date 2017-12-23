using System;
using System.Linq;
using System.Threading.Tasks;
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

      var seasonToUpdate = _context.Season.Single(s => s.Id == season.Id);
      if (season.WinningTeamId.HasValue) {
        seasonToUpdate.RelatedSeasonResult = new SeasonResult {
          Season = season.Id,
          WinningTeam = season.WinningTeamId.Value
        };
      }
      else _context.SeasonResult.RemoveRange(_context.SeasonResult.Where(sr => sr.Season == season.Id));
      _context.Update(seasonToUpdate);
      await _context.SaveChangesAsync();
    }
  }
}