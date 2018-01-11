using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public class DeleteSeasonEntryValidator : MotorsportsValidator<SeasonEntry, SeasonEntry.SeasonEntryKey>, IDeleteValidator<SeasonEntry> {
    readonly MotorsportsContext _context;

    public DeleteSeasonEntryValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Team)
        .Must(NotHaveWonAnyRounds)
        .WithMessage("This entry cannot be deleted, because the team has won rounds.")
        .Must(NotHaveWonSeason)
        .WithMessage("This entry cannot be deleted, because the team has won the season.");
    }

    bool NotHaveWonSeason(SeasonEntry seasonEntry, int teamId) {
      return !_context.Season.Any(r => r.Id == seasonEntry.Season && r.WinningTeam == teamId);
    }

    bool NotHaveWonAnyRounds(SeasonEntry seasonEntry, int teamId) {
      return !_context.Round.Any(r => r.Season == seasonEntry.Season && r.WinningTeam == teamId);
    }
  }
}