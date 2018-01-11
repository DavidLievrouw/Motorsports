using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateSeasonEntryValidator : MotorsportsValidator<SeasonEntry, SeasonEntry.SeasonEntryKey>, ICreateValidator<SeasonEntry> {
    readonly MotorsportsContext _context;

    public CreateSeasonEntryValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Season)
        .NotEmpty()
        .WithMessage("A season is required.")
        .Must(SeasonExists)
        .WithMessage("The specified season does not exist.");

      RuleFor(_ => _.Team)
        .NotEmpty()
        .WithMessage("A team is required.")
        .Must(BeUnique)
        .WithMessage("An entry for this season and team already exists.")
        .Must(TeamExists)
        .WithMessage("The specified team does not exist.");

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");
    }

    bool SeasonExists(SeasonEntry seasonEntry, int season) {
      return _context.Season.Any(_ => _.Id == season);
    }
    
    bool TeamExists(SeasonEntry seasonEntry, int team) {
      return _context.Team.Any(_ => _.Id == team);
    }

    bool BeUnique(SeasonEntry seasonEntry, int season) {
      return !_context.SeasonEntry.Any(_ => 
        _.Season == seasonEntry.Season && 
        _.Team == seasonEntry.Team);
    }
  }
}