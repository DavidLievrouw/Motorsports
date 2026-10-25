using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public class DeleteSeasonValidator : MotorsportsValidator<Season, int>, IDeleteValidator<Season> {
      private readonly MotorsportsContext _context;

    public DeleteSeasonValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Id)
        .NotEmpty()
        .WithMessage("A season is required.")
        .Must(NotHaveAnyRounds)
        .WithMessage("This season cannot be deleted, because it has rounds.");
    }

    private bool NotHaveAnyRounds(Season season, int seasonId) {
      return !_context.Round.Any(r => r.Season == seasonId);
    }
  }
}