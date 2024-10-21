using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateSportValidator : MotorsportsValidator<Sport, string>, ICreateValidator<Sport> {
    readonly MotorsportsContext _context;

    public CreateSportValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Name)
        .Must(BeUnique)
        .WithMessage("A sport with the specified name already exists.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool BeUnique(Sport sport, string name) {
      return !_context.Sport.Any(s => EF.Functions.Like(s.Name, name));
    }
  }
}