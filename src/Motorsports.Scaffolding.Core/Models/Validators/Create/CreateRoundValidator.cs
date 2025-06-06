﻿using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateRoundValidator : MotorsportsValidator<Round, int>, ICreateValidator<Round> {
    readonly MotorsportsContext _context;

    public CreateRoundValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Date)
        .Must(date => date > new DateTime(2010, 1, 1))
        .WithMessage("A date is required.");

      RuleFor(_ => _.Number)
        .Must(number => number > -1 && number < 100)
        .WithMessage("A valid number is required.")
        .Must(BeUniqueOrRound0)
        .WithMessage("This round already exists.");
      
      RuleFor(_ => _.Venue)
        .NotEmpty()
        .WithMessage("A venue is required.")
        .Must(VenueExists)
        .WithMessage("The specified venue does not exist.");
      
      RuleFor(_ => _.Status)
        .NotEmpty()
        .WithMessage("A status is required.")
        .Must(StatusExists)
        .WithMessage("The specified status does not exist.");

      RuleFor(_ => _.Season)
        .Must(number => number != default(int))
        .WithMessage("A season is required.")
        .Must(SeasonExists)
        .WithMessage("The specified season does not exist.");
    }

    bool VenueExists(Round round, string venue) {
      return _context.Venue.Any(_ => EF.Functions.Like(_.Name, venue));
    }
    
    bool StatusExists(Round round, string status) {
      return _context.Status.Any(_ => EF.Functions.Like(_.Name, status));
    }

    bool SeasonExists(Round round, int season) {
      return _context.Season.Any(_ => _.Id == season);
    }

    bool BeUniqueOrRound0(Round round, short number) {
      return number == 0 || !_context.Round.Any(_ => 
        _.Season == round.Season && 
        _.Number == number);
    }
  }
}