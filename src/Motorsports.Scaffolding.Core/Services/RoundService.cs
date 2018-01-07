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
  public interface IRoundService {
    Task<Round> LoadDataRecord(int roundId);
    Task<RoundDisplayModel> CreateForRound(Round round);
    Task<RoundDisplayModel> GetNew(int seasonId);
    Task<List<RoundDisplayModel>> LoadRoundList();
    Task<List<RoundDisplayModel>> LoadRoundList(int seasonId);
    Task<RoundDisplayModel> LoadDisplayModel(int roundId);
    Task UpdateRound(RoundEditModel round);
    Task PersistRound(Round round);
    Task DeleteRound(int roundId);
    Task<IEnumerable<EventHistoryItem>> GetEventHistory(int roundId);
  }

  public class RoundService : IRoundService {
    readonly MotorsportsContext _context;
    readonly IQueryExecutor _queryExecutor;

    public RoundService(
      MotorsportsContext context,
      IQueryExecutor queryExecutor) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public Task<Round> LoadDataRecord(int roundId) {
      return _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .SingleOrDefaultAsync(m => m.Id == roundId);
    }

    public Task<RoundDisplayModel> CreateForRound(Round round) {
      return Task.FromResult(
        new RoundDisplayModel(
          new Round {
            Id = round.Id,
            Date = round.Date,
            Season = round.Season,
            RelatedSeason = _context.Season.Include(s => s.RelatedRounds).SingleOrDefault(s => s.Id == round.Season),
            Name = round.Name,
            Number = round.Number,
            Venue = round.Venue,
            Rain = round.Rain,
            Rating = round.Rating,
            Status = round.Status,
            WinningTeam = round.WinningTeam,
            RelatedRoundWinners = round.Id != default(int)
              ? _context.RoundWinner.Where(w => w.Round == round.Id).ToList()
              : null,
            RelatedStatus = !string.IsNullOrEmpty(round.Status)
              ? _context.Status.SingleOrDefault(s => s.Name == round.Status)
              : null,
            RelatedVenue = !string.IsNullOrEmpty(round.Venue)
              ? _context.Venue.SingleOrDefault(v => StringComparer.InvariantCultureIgnoreCase.Equals(v.Name, round.Venue))
              : null,
            RelatedWinningTeam = round.WinningTeam.HasValue
              ? _context.Team.Single(t => t.Id == round.WinningTeam.Value)
              : null
          },
          _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
          _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
          _context.Status,
          _context.Venue.OrderBy(v => v.Name)));
    }

    public async Task<RoundDisplayModel> GetNew(int seasonId) {
      var lastRoundInSeason = await _context.Round
        .Where(r => r.Season == seasonId)
        .OrderByDescending(r => r.Date)
        .FirstOrDefaultAsync();
      return new RoundDisplayModel(
        new Round {
          Date = lastRoundInSeason?.Date ?? DateTime.Now.Date,
          Season = seasonId,
          RelatedSeason = _context.Season.Include(s => s.RelatedRounds).Single(s => s.Id == seasonId),
          Status = RoundStatus.Scheduled.ToString(),
          Number = (short)((lastRoundInSeason?.Number ?? 0) + 1),
        },
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
        _context.Status,
        _context.Venue.OrderBy(v => v.Name));
    }

    public Task<List<RoundDisplayModel>> LoadRoundList() {
      return _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .OrderBy(r => r.Number)
        .Select(r => new RoundDisplayModel(r, null, null, null, null))
        .ToListAsync();
    }

    public Task<List<RoundDisplayModel>> LoadRoundList(int seasonId) {
      return _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .Where(r => r.Season == seasonId)
        .OrderBy(r => r.Number)
        .Select(r => new RoundDisplayModel(r, null, null, null, null))
        .ToListAsync();
    }

    public async Task<RoundDisplayModel> LoadDisplayModel(int roundId) {
      var roundDataModel = await _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .SingleOrDefaultAsync(m => m.Id == roundId);

      return new RoundDisplayModel(
        roundDataModel,
        _context.Team.Where(team => team.Sport == roundDataModel.RelatedSeason.Sport).OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
        _context.Status,
        _context.Venue.OrderBy(v => v.Name));
    }

    public async Task UpdateRound(RoundEditModel round) {
      if (round == null) throw new ArgumentNullException(nameof(round));

      // Update own props
      var roundToUpdate = _context.Round.Single(s => s.Id == round.Id);
      roundToUpdate.Season = round.Season;
      roundToUpdate.Venue = round.Venue;
      roundToUpdate.Date = round.Date;
      roundToUpdate.Number = round.Number;
      roundToUpdate.Name = round.Name;
      roundToUpdate.Rain = round.Rain.HasValue
        ? (int) round.Rain.Value
        : new int?();
      roundToUpdate.Status = round.Status.ToString();
      roundToUpdate.Rating = round.Rating;
      roundToUpdate.WinningTeam = round.WinningTeamId;
      _context.Update(roundToUpdate);
      await _context.SaveChangesAsync();
      
      // Update winners
      using (var transactionalQueryExecutor = _queryExecutor.BeginTransaction()) {
        try {
          await transactionalQueryExecutor
            .NewQuery("DELETE FROM [dbo].[RoundWinner] WHERE [Round]=@Round")
            .WithCommandType(CommandType.Text)
            .WithParameters(new {Round = round.Id})
            .ExecuteAsync();
          var winnersToAdd = round.WinningParticipantIds.Select(
            wp => new RoundWinner {
              Round = round.Id,
              Participant = wp
            });
          foreach (var winner in winnersToAdd) {
            await transactionalQueryExecutor
              .NewQuery(
                @"INSERT INTO [dbo].[RoundWinner]
                         ([Round]
                         ,[Participant])
                  VALUES
                         (@Round
                         ,@Participant)")
              .WithCommandType(CommandType.Text)
              .WithParameters(new {Round = winner.Round, Participant = winner.Participant})
              .ExecuteAsync();
          }
          transactionalQueryExecutor.Commit();
        } catch {
          transactionalQueryExecutor.Rollback();
          throw;
        }
      }
    }
    
    public async Task<IEnumerable<EventHistoryItem>> GetEventHistory(int roundId) {
      var round = await _context.Round
        .Include(r => r.RelatedSeason)
        .Where(r => r.Id == roundId)
        .FirstOrDefaultAsync();
      if (round == null) throw new ArgumentException("The specified round was not found.", nameof(roundId));

      return await _queryExecutor.NewQuery(@"
        SELECT TOP 5
	        R.[Id],
	        R.[Date],
	        R.[Rating],
	        R.[Rain],
	        T.[Name] AS WinningTeam,
	        STRING_AGG(P.[FirstName] + ' ' + P.[LastName], ', ') WITHIN GROUP (ORDER BY P.[LastName] ASC, P.[FirstName] ASC) AS WinningParticipants
        FROM
	        [dbo].[Round] R
	        INNER JOIN [dbo].[Season] S ON R.[Season] = S.[Id]
	        LEFT JOIN [dbo].[Team] T ON R.[WinningTeam] = T.[Id]
	        LEFT JOIN [dbo].[RoundWinner] RW ON R.[Id] = RW.[Round]
	        LEFT JOIN [dbo].[Participant] P ON P.[Id] = RW.[Participant]
        WHERE
	        R.[Venue] = @Venue
	        AND R.[Status] <> 'Scheduled'
	        AND S.[Sport] = @Sport
          AND R.[Date] < @Date
        GROUP BY
	        R.[Id],
	        R.[Date],
	        R.[Rating],
	        R.[Rain],
	        T.[Name]
        ORDER BY
        	R.[Date] DESC")
        .WithCommandType(CommandType.Text)
        .WithParameters(new {
          Venue = round.Venue,
          Sport = round.RelatedSeason.Sport,
          Date = round.Date.Date
        })
        .ExecuteAsync<EventHistoryItem>();
    }

    public async Task PersistRound(Round round) {
      _context.Add(round);
      await _context.SaveChangesAsync();
    }

    public async Task DeleteRound(int roundId) {
      var round = await _context.Round.SingleOrDefaultAsync(m => m.Id == roundId);
      _context.Round.Remove(round);
      await _context.SaveChangesAsync();
    }
  }
}