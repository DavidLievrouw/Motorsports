using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motorsports.Scaffolding.Core.Models;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IRoundService {
    Task UpdateRound(RoundEditModel round);
  }

  public class RoundService : IRoundService {
    readonly MotorsportsContext _context;

    public RoundService(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task UpdateRound(RoundEditModel round) {
      if (round == null) throw new ArgumentNullException(nameof(round));

      // Find record to update
      var roundToUpdate = _context.Round
        .Include(s => s.RelatedRoundResult)
        .Include(s => s.RelatedRoundWinners)
        .Single(s => s.Id == round.Id);

      // Update round result
      if (roundToUpdate.RelatedRoundResult == null && round.WinningTeamId.HasValue ||
          roundToUpdate.RelatedRoundResult?.WinningTeam != round.WinningTeamId ||
          roundToUpdate.RelatedRoundResult?.Rain != (round.Rain.HasValue? (decimal) round.Rain: new decimal?()) ||
          roundToUpdate.RelatedRoundResult?.Status != round.Status?.ToString() ||
          roundToUpdate.RelatedRoundResult?.Rating != round.Rating) {
        _context.RoundResult.RemoveRange(_context.RoundResult.Where(sr => sr.Round == round.Id));
        if (round.WinningTeamId.HasValue) {
          roundToUpdate.RelatedRoundResult = new RoundResult {
            Round = round.Id,
            WinningTeam = round.WinningTeamId.Value,
            Rain = round.Rain.HasValue
              ? (decimal) round.Rain
              : new decimal?(),
            Status = round.Status.HasValue
              ? round.Status.ToString()
              : null,
            Rating = round.Rating
          };
        }
      }

      // Update winners
      _context.RoundWinner.RemoveRange(_context.RoundWinner.Where(sw => sw.Round == round.Id));
      foreach (var winningParticipantId in round.WinningParticipantIds) {
        _context.RoundWinner.Add(
          new RoundWinner {
            Round = round.Id,
            Participant = winningParticipantId
          });
      }

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
  }
}