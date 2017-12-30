using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateRoundValidator : MotorsportsValidator<Round>, IUpdateValidator<Round> {
    readonly MotorsportsContext _context;

    public UpdateRoundValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Date)
        .Must(date => date > new DateTime(2010, 1, 1))
        .WithMessage("A date is required.");

      RuleFor(_ => _.Number)
        .Must(number => number > 0 && number < 100)
        .WithMessage("A valid number is required.")
        .Must(MustExist)
        .WithMessage("The round to update does not exist.")
        .Must(BeUnique)
        .WithMessage("This round already exists.");
      
      RuleFor(_ => _.Venue)
        .NotEmpty()
        .WithMessage("A venue is required.")
        .Must(VenueExists)
        .WithMessage("The specified venue does not exist.");
      
      RuleFor(_ => _.Season)
        .NotEmpty()
        .WithMessage("A season is required.")
        .Must(SeasonExists)
        .WithMessage("The specified season does not exist.");
    }
    
    bool MustExist(Round round, short number) {
      return _context.Round.Any(_ => round.Id == _.Id);
    }

    bool VenueExists(Round round, string venue) {
      return _context.Venue.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, venue));
    }
    
    bool SeasonExists(Round round, int season) {
      return _context.Season.Any(_ => _.Id == season);
    }

    bool BeUnique(Round round, short number) {
      return !_context.Round.Any(_ => 
        StringComparer.InvariantCultureIgnoreCase.Equals(_.Season, round.Season) && 
        StringComparer.InvariantCultureIgnoreCase.Equals(_.Number, number) &&
        _.Id != round.Id);
    }
  }
}