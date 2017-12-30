using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateVenueValidator : MotorsportsValidator<Venue>, IUpdateValidator<Venue> {
    readonly MotorsportsContext _context;

    public UpdateVenueValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.");

      RuleFor(_ => _.Name)
        .Must(MustExist)
        .WithMessage("The venue to update does not exist.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool MustExist(Venue venue, string name) {
      return _context.Venue.SingleOrDefault(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, name)) != null;
    }
  }
}