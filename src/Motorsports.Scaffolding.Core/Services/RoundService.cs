using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.DisplayModels;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IRoundService {
    Task<RoundDisplayModel> GetNew();
    Task<List<RoundDisplayModel>> LoadRoundList();
    Task<RoundDisplayModel> LoadDisplayModel(int roundId);
    Task UpdateRound(RoundEditModel round);
    Task CreateRound(Round round);
    Task DeleteRound(int roundId);
  }

  public class RoundService : IRoundService {
    readonly MotorsportsContext _context;

    public RoundService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<RoundDisplayModel> GetNew() {
      return Task.FromResult(
        new RoundDisplayModel(
          new Round {Date = DateTime.Today},
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

      // Find record to update
      var roundToUpdate = _context.Round
        .Include(s => s.RelatedSeason)
        .Include(r => r.RelatedVenue)
        .Include(s => s.RelatedRoundResult)
        .AsNoTracking()
        .Include(s => s.RelatedRoundWinners)
        .AsNoTracking()
        .Single(s => s.Id == round.Id);

      // Update round result
      if (roundToUpdate.RelatedRoundResult == null && round.WinningTeamId.HasValue ||
          roundToUpdate.RelatedRoundResult?.WinningTeam != round.WinningTeamId ||
          roundToUpdate.RelatedRoundResult?.Rain !=
          (round.Rain.HasValue
            ? (decimal) round.Rain
            : new decimal?()) ||
          roundToUpdate.RelatedRoundResult?.Status != round.Status?.ToString() ||
          roundToUpdate.RelatedRoundResult?.Rating != round.Rating) {
        var resultToUpdate = _context.RoundResult.SingleOrDefault(r => r.Round == roundToUpdate.Id);
        if (resultToUpdate == null) {
          roundToUpdate.RelatedRoundResult = new RoundResult {
            Round = round.Id,
            WinningTeam = round.WinningTeamId,
            Rain = round.Rain.HasValue
              ? (decimal) round.Rain
              : new decimal?(),
            Status = round.Status.HasValue
              ? round.Status.ToString()
              : null,
            Rating = round.Rating
          };
          _context.RoundResult.Add(roundToUpdate.RelatedRoundResult);
        }
        else {
          resultToUpdate.Round = round.Id;
          resultToUpdate.WinningTeam = round.WinningTeamId;
          resultToUpdate.Rain = round.Rain.HasValue
            ? (decimal) round.Rain
            : new decimal?();
          resultToUpdate.Status = round.Status.HasValue
            ? round.Status.ToString()
            : null;
          resultToUpdate.Rating = round.Rating;
        }
      }

      // Update winners
      var winnersToRemove = _context.RoundWinner.AsNoTracking().Where(rw => rw.Round == roundToUpdate.Id);
      var winnersToAdd = round.WinningParticipantIds.Select(
        wp => new RoundWinner {
          Round = round.Id,
          Participant = wp
        });
      _context.RoundWinner.RemoveRange(winnersToRemove);
      roundToUpdate.RelatedRoundWinners = winnersToAdd.ToList();

      // Update venue
      roundToUpdate.Venue = round.Venue;

      // Update properties
      roundToUpdate.Date = round.Date;
      roundToUpdate.Number = round.Number;
      roundToUpdate.Name = round.Name;

      // Commit
      _context.Update(roundToUpdate);
      await _context.SaveChangesAsync();
    }

    public async Task CreateRound(Round round) {
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