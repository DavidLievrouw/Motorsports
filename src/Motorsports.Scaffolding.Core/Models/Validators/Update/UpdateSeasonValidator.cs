using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateSeasonValidator : MotorsportsValidator<Season, int>, IUpdateValidator<Season, int> {
    readonly MotorsportsContext _context;

    public UpdateSeasonValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Label)
        .Must(MustExist)
        .WithMessage("The season to update does not exist.");

      RuleFor(_ => _.Sport)
        .NotEmpty()
        .WithMessage("A sport is required.")
        .Must(SportExists)
        .WithMessage("The specified sport does not exist.");
    }

    bool MustExist(Season season, string label) {
      return _context.Season.Any(_ => season.Id == _.Id);
    }
    
    bool SportExists(Season season, string sport) {
      return _context.Sport.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, sport));
    }
  }
}