using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateVenueValidator : MotorsportsValidator<Venue>, ICreateValidator<Venue> {
    readonly MotorsportsContext _context;

    public CreateVenueValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.");

      RuleFor(_ => _.Name)
        .Must(BeUnique)
        .WithMessage("A venue with the specified name already exists.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool BeUnique(Venue venue, string name) {
      return !_context.Venue.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, name));
    }
  }
}