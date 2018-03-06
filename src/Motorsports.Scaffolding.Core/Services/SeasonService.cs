using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Dapper;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface ISeasonService {
    Task<Season> LoadDataRecord(int seasonId);
    Task<SeasonDisplayModel> GetNew();
    Task<SeasonsIndexDisplayModel> LoadSeasonList();
    Task<SeasonsIndexDisplayModel> LoadSeasonList(string sport);
    Task<SeasonDisplayModel> LoadDisplayModel(int seasonId);
    Task UpdateSeason(SeasonEditModel season);
    Task PersistSeason(Season season);
    Task DeleteSeason(int seasonId);
  }

  public class SeasonService : ISeasonService {
    readonly MotorsportsContext _context;
    readonly IQueryExecutor _queryExecutor;

    public SeasonService(
      MotorsportsContext context,
      IQueryExecutor queryExecutor) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public Task<Season> LoadDataRecord(int seasonId) {
      return _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedWinningTeam)
        .ThenInclude(t => t.RelatedSeasonEntries)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .SingleOrDefaultAsync(m => m.Id == seasonId);
    }

    public Task<SeasonDisplayModel> GetNew() {
      return Task.FromResult(
        new SeasonDisplayModel(
          new Season(),
          _context.Sport.OrderBy(sport => sport.Name),
          _context.SeasonEntry.Include(se => se.RelatedTeam).OrderBy(se => se.RelatedTeam.Sport).ThenBy(team => team.Name),
          _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName)));
    }

    public Task<SeasonsIndexDisplayModel> LoadSeasonList() {
      return LoadSeasonList(null);
    }

    public async Task<SeasonsIndexDisplayModel> LoadSeasonList(string sport) {
      var allSeasons = await _context.Season
        .Where(s => sport == null || StringComparer.InvariantCultureIgnoreCase.Equals(s.Sport, sport))
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedWinningTeam)
        .ThenInclude(t => t.RelatedSeasonEntries)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .Select(s => new SeasonDisplayModel(s, null, null, null))
        .ToListAsync();

      return new SeasonsIndexDisplayModel {
        SeasonsPerSport = allSeasons
          .GroupBy(s => s.RelatedSport)
          .Select(
            group => new {
              Sport = group.Key,
              Seasons = group.OrderByDescending(s => s.StartDate ?? DateTime.MaxValue).AsEnumerable()
            })
          .ToDictionary(_ => _.Sport, _ => _.Seasons)
      };
    }

    public async Task<SeasonDisplayModel> LoadDisplayModel(int seasonId) {
      var seasonDataModel = await _context.Season
        .Include(s => s.RelatedSport)
        .Include(s => s.RelatedWinningTeam)
        .ThenInclude(t => t.RelatedSeasonEntries)
        .Include(s => s.RelatedSeasonWinners)
        .ThenInclude(sw => sw.RelatedParticipant)
        .Include(s => s.RelatedRounds)
        .SingleOrDefaultAsync(m => m.Id == seasonId);

      return new SeasonDisplayModel(
        seasonDataModel,
        _context.Sport.OrderBy(sport => sport.Name),
        _context.SeasonEntry.Include(se => se.RelatedTeam).Where(se => se.Season == seasonId).OrderBy(se => se.RelatedTeam.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName));
    }

    public async Task UpdateSeason(SeasonEditModel season) {
      if (season == null) throw new ArgumentNullException(nameof(season));

      // Find record to update
      var seasonToUpdate = _context.Season.Single(s => s.Id == season.Id);

      // Update own props
      seasonToUpdate.Label = season.Label;
      seasonToUpdate.WinningTeam = season.WinningTeamId;
      _context.Update(seasonToUpdate);
      await _context.SaveChangesAsync();

      // Update winners
      using (var transactionalQueryExecutor = _queryExecutor.BeginTransaction()) {
        try {
          await transactionalQueryExecutor
            .NewQuery("DELETE FROM [dbo].[SeasonWinner] WHERE [Season]=@Season")
            .WithCommandType(CommandType.Text)
            .WithParameters(new {Season = season.Id})
            .ExecuteAsync();
          var winnersToAdd = season.WinningParticipantIds.Select(
            wp => new SeasonWinner {
              Season = season.Id,
              Participant = wp
            });
          foreach (var winner in winnersToAdd) {
            await transactionalQueryExecutor
              .NewQuery(
                @"
              INSERT INTO [dbo].[SeasonWinner]
                         ([Season]
                         ,[Participant])
                   VALUES
                         (@Season
                         ,@Participant)")
              .WithCommandType(CommandType.Text)
              .WithParameters(new {Season = winner.Season, Participant = winner.Participant})
              .ExecuteAsync();
          }

          transactionalQueryExecutor.Commit();
        } catch {
          transactionalQueryExecutor.Rollback();
          throw;
        }
      }
    }

    public async Task PersistSeason(Season season) {
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