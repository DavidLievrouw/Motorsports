using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonEntryService {
    Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId);
    Task<SeasonEntryDisplayModel> GetNew(int seasonId);
    Task PersistSeasonEntry(SeasonEntry seasonEntry);
  }

  public class SeasonEntryService : ISeasonEntryService {
    readonly MotorsportsContext _context;

    public SeasonEntryService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId) {
      return _context.SeasonEntry
        .Include(se => se.RelatedTeam)
        .Include(se => se.RelatedSeason)
        .ThenInclude(s => s.RelatedRounds)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Where(se => se.Season == seasonId)
        .OrderBy(se => se.RelatedTeam.Name)
        .ThenBy(se => se.Name)
        .ToListAsync();
    }

    public async Task<SeasonEntryDisplayModel> GetNew(int seasonId) {
      var season = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .SingleOrDefaultAsync(m => m.Id == seasonId);

      return new SeasonEntryDisplayModel(
        new SeasonEntry {
          Season = seasonId,
          RelatedSeason = season
        },
        _context.Team.Where(team => team.Sport == season.Sport).OrderBy(team => team.Sport).ThenBy(team => team.Name));
    }

    public async Task PersistSeasonEntry(SeasonEntry seasonEntry) {
      _context.Add(seasonEntry);
      await _context.SaveChangesAsync();
    }
  }
}