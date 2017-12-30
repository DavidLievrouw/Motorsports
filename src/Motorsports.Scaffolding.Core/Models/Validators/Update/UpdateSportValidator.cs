using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateSportValidator : MotorsportsValidator<Sport>, IUpdateValidator<Sport> {
    readonly MotorsportsContext _context;

    public UpdateSportValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Name)
        .Must(MustExist)
        .WithMessage("The sport to update does not exist.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool MustExist(Sport sport, string name) {
      return _context.Sport.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, name));
    }
  }
}