using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonEntryService {
    Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId);
    Task<SeasonEntryDisplayModel> GetNew(int seasonId);
    Task PersistSeasonEntry(SeasonEntry seasonEntry);
    Task<SeasonEntryDisplayModel> LoadDisplayModel(int seasonId, int teamId);
    Task DeleteSeasonEntry(int seasonId, int teamId);
    Task UpdateSeasonEntry(SeasonEntryEditModel seasonEntry);
  }

  public class SeasonEntryService : ISeasonEntryService {
    readonly MotorsportsContext _context;
    readonly IQueryExecutor _queryExecutor;

    public SeasonEntryService(
      MotorsportsContext context,
      IQueryExecutor queryExecutor) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public Task<List<SeasonEntry>> LoadSeasonEntryList(int seasonId) {
      return _context.SeasonEntry
        .Include(se => se.RelatedTeam)
        .ThenInclude(t => t.RelatedCountry)
        .Include(se => se.RelatedSeason)
        .ThenInclude(s => s.RelatedRounds)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(se => se.RelatedSeason)
        .ThenInclude(s => s.RelatedSport)
        .Where(se => se.Season == seasonId)
        .OrderBy(se => se.RelatedTeam.Name)
        .ThenBy(se => se.Name)
        .AsNoTracking()
        .ToListAsync();
    }

    public async Task<SeasonEntryDisplayModel> GetNew(int seasonId) {
      var season = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .AsNoTracking()
        .SingleOrDefaultAsync(m => m.Id == seasonId);

      return new SeasonEntryDisplayModel(
        new SeasonEntry {
          Season = seasonId,
          RelatedSeason = season
        },
        _context.Team
          .Where(team => team.Sport == season.Sport)
          .OrderBy(team => team.Sport)
          .ThenBy(team => team.Name)
          .AsNoTracking()
        );
    }

    public async Task PersistSeasonEntry(SeasonEntry seasonEntry) {
      _context.Add(seasonEntry);
      await _context.SaveChangesAsync();
    }

    public async Task<SeasonEntryDisplayModel> LoadDisplayModel(int seasonId, int teamId) {
      var seasonEntry = await _context.SeasonEntry
        .Include(se => se.RelatedTeam)
        .ThenInclude(t => t.RelatedCountry)
        .Include(se => se.RelatedSeason)
        .ThenInclude(s => s.RelatedRounds)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedWinningTeam)
        .Include(s => s.RelatedSeason)
        .ThenInclude(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(se => se.RelatedSeason)
        .ThenInclude(s => s.RelatedSport)
        .Where(se => se.Season == seasonId && se.Team == teamId)
        .AsNoTracking()
        .SingleOrDefaultAsync();

      return seasonEntry == null
        ? null
        : new SeasonEntryDisplayModel(seasonEntry);
    }

    public async Task DeleteSeasonEntry(int seasonId, int teamId) {
      var seasonEntry = await _context.SeasonEntry.SingleOrDefaultAsync(m => m.Season == seasonId && m.Team == teamId);
      _context.SeasonEntry.Remove(seasonEntry);
      await _context.SaveChangesAsync();
    }

    public async Task UpdateSeasonEntry(SeasonEntryEditModel seasonEntry) {
      if (seasonEntry == null) throw new ArgumentNullException(nameof(seasonEntry));

      await _queryExecutor
        .NewQuery("UPDATE [dbo].[SeasonEntry] SET Name=@Name WHERE [Season]=@Season AND [Team]=@Team")
        .WithCommandType(CommandType.Text)
        .WithParameters(new {
          Season = seasonEntry.Season,
          Team = seasonEntry.Team,
          Name = seasonEntry.Name
        })
        .ExecuteAsync();
    }
  }
}