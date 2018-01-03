using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateSeasonValidator : MotorsportsValidator<Season, int>, ICreateValidator<Season> {
    readonly MotorsportsContext _context;

    public CreateSeasonValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Sport)
        .NotEmpty()
        .WithMessage("A sport is required.")
        .Must(SportExists)
        .WithMessage("The specified sport does not exist.");
    }

    bool SportExists(Season season, string sport) {
      return _context.Sport.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, sport));
    }
  }
}