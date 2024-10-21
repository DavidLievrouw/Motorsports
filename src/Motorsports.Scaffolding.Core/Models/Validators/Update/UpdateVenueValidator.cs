using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateVenueValidator : MotorsportsValidator<Venue, string>, IUpdateValidator<Venue, string> {
    readonly MotorsportsContext _context;

    public UpdateVenueValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.")
        .Must(CountryExists)
        .WithMessage("The specified country does not exist.");

      RuleFor(_ => _.Name)
        .Must(MustExist)
        .WithMessage("The venue to update does not exist.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool MustExist(Venue venue, string name, ValidationContext<Venue> context) {
      var existingKey = GetKey(context);
      return _context.Venue.Any(_ => EF.Functions.Like(_.Name, existingKey));
    }

    bool CountryExists(Venue venue, string country) {
      return _context.Country.Any(c => EF.Functions.Like(c.Iso, country));
    }
  }
}