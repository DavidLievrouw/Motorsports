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
        .Include(r => r.RelatedRoundResult)
        .ThenInclude(rr => rr.RelatedWinningTeam)
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
            RelatedSeason = _context.Season.SingleOrDefault(s => s.Id == round.Season),
            Name = round.Name,
            Number = round.Number,
            Venue = round.Venue,
            RelatedRoundResult = round.Id != default(int)
              ? _context.RoundResult.SingleOrDefault(r => r.Round == round.Id)
              : null,
            RelatedRoundWinners = round.Id != default(int)
              ? _context.RoundWinner.Where(w => w.Round == round.Id).ToList()
              : null,
            RelatedVenue = !string.IsNullOrEmpty(round.Venue)
              ? _context.Venue.SingleOrDefault(v => StringComparer.InvariantCultureIgnoreCase.Equals(v.Name, round.Venue))
              : null
          },
          _context.Season.Include(s => s.RelatedRounds).OrderBy(season => season.Sport),
          _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
          _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
          _context.Status,
          _context.Venue.OrderBy(v => v.Name)));
    }

    public Task<RoundDisplayModel> GetNew(int seasonId) {
      return Task.FromResult(
        new RoundDisplayModel(
          new Round {Date = DateTime.Today, Season = seasonId, RelatedSeason = _context.Season.Single(s => s.Id == seasonId)},
          _context.Season.Include(s => s.RelatedRounds).OrderBy(season => season.Sport),
          _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
          _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
          _context.Status,
          _context.Venue.OrderBy(v => v.Name)));
    }

    public Task<List<RoundDisplayModel>> LoadRoundList() {
      return _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedRoundResult)
        .ThenInclude(rr => rr.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .Select(r => new RoundDisplayModel(r, null, null, null, null, null))
        .ToListAsync();
    }

    public Task<List<RoundDisplayModel>> LoadRoundList(int seasonId) {
      return _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedRoundResult)
        .ThenInclude(rr => rr.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .Where(r => r.Season == seasonId)
        .Select(r => new RoundDisplayModel(r, null, null, null, null, null))
        .ToListAsync();
    }

    public async Task<RoundDisplayModel> LoadDisplayModel(int roundId) {
      var seasonDataModel = await _context.Round
        .Include(r => r.RelatedSeason)
        .Include(r => r.RelatedRoundResult)
        .ThenInclude(rr => rr.RelatedWinningTeam)
        .Include(r => r.RelatedRoundWinners)
        .ThenInclude(rw => rw.RelatedParticipant)
        .Include(r => r.RelatedVenue)
        .SingleOrDefaultAsync(m => m.Id == roundId);

      return new RoundDisplayModel(
        seasonDataModel,
        _context.Season.Include(s => s.RelatedRounds).OrderBy(season => season.Sport),
        _context.Team.OrderBy(team => team.Sport).ThenBy(team => team.Name),
        _context.Participant.OrderBy(participant => participant.LastName).ThenBy(participant => participant.FirstName),
        _context.Status,
        _context.Venue.OrderBy(v => v.Name));
    }

    public async Task UpdateRound(RoundEditModel round) {
      if (round == null) throw new ArgumentNullException(nameof(round));

      // Save EF changes
      var roundToUpdate = _context.Round.Single(s => s.Id == round.Id);
      roundToUpdate.Venue = round.Venue;
      roundToUpdate.Date = round.Date;
      roundToUpdate.Number = round.Number;
      roundToUpdate.Name = round.Name;
      _context.Update(roundToUpdate);
      await _context.SaveChangesAsync();

      var transactionalQueryExecutor = _queryExecutor.BeginTransaction();
      try {
        // Update round result
        await _queryExecutor
          .NewQuery("DELETE FROM [dbo].[RoundResult] WHERE [Round]=@Round")
          .WithCommandType(CommandType.Text)
          .WithParameters(new {Round = round.Id})
          .ExecuteAsync();
        if (round.Status.HasValue) {
          await _queryExecutor
            .NewQuery(@"
              INSERT INTO [dbo].[RoundResult]
                         ([Round]
                         ,[Status]
                         ,[Rating]
                         ,[Rain]
                         ,[WinningTeam])
                   VALUES
                         (@Round
                         ,@Status
                         ,@Rating
                         ,@Rain
                         ,@WinningTeam)")
            .WithCommandType(CommandType.Text)
            .WithParameters(
              new {
                Round = round.Id,
                Status = round.Status.ToString(),
                Rating = round.Rating,
                Rain = round.Rain.HasValue
                  ? (int) round.Rain.Value
                  : new int?(),
                WinningTeam = round.WinningTeamId
              })
            .ExecuteAsync();
        }

        // Update winners
        await _queryExecutor
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
          await _queryExecutor
            .NewQuery(@"
              INSERT INTO [dbo].[RoundWinner]
                         ([Round]
                         ,[Participant])
                   VALUES
                         (@Round
                         ,@Participant)")
            .WithCommandType(CommandType.Text)
            .WithParameters(new {Round = winner.Round, Participant = winner.Participant})
            .ExecuteAsync();
        }

        // Commit
        transactionalQueryExecutor.Commit();
      } catch {
        transactionalQueryExecutor.Rollback();
        throw;
      } finally {
        transactionalQueryExecutor.Dispose();
      }
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