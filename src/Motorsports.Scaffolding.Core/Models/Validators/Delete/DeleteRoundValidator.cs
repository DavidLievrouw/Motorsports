using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public class DeleteRoundValidator : MotorsportsValidator<Round, int>, IDeleteValidator<Round> {
    readonly MotorsportsContext _context;

    public DeleteRoundValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Id)
        .NotEmpty()
        .WithMessage("A round is required.")
        .Must(NotHaveAnyWinners)
        .WithMessage("This round cannot be deleted, because it has winners.");
    }

    bool NotHaveAnyWinners(Round season, int roundId) {
      return !_context.RoundWinner.Any(r => r.Round == roundId) && !_context.Round.Single(r => r.Id == roundId).WinningTeam.HasValue;
    }
  }
}