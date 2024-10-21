using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateVenueValidator : MotorsportsValidator<Venue, string>, ICreateValidator<Venue> {
    readonly MotorsportsContext _context;

    public CreateVenueValidator(MotorsportsContext context) {
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
        .Must(BeUnique)
        .WithMessage("A venue with the specified name already exists.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool BeUnique(Venue venue, string name) {
      return !_context.Venue.Any(_ => EF.Functions.Like(_.Name, name));
    }    

    bool CountryExists(Venue venue, string country) {
      return _context.Country.Any(_ => EF.Functions.Like(_.Iso, country));
    }
  }
}