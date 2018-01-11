using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateSeasonEntryValidator : MotorsportsValidator<SeasonEntry, SeasonEntry.SeasonEntryKey>, IUpdateValidator<SeasonEntry, SeasonEntry.SeasonEntryKey> {
    readonly MotorsportsContext _context;

    public UpdateSeasonEntryValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Season)
        .NotEmpty()
        .WithMessage("A season is required.")
        .Must(SeasonExists)
        .WithMessage("The specified season does not exist.");

      RuleFor(_ => _.Team)
        .NotEmpty()
        .WithMessage("A team is required.")
        .Must(TeamExists)
        .WithMessage("The specified team does not exist.");

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.")
        .Must(MustExist)
        .WithMessage("The season entry to update does not exist.");
    }

    bool MustExist(SeasonEntry seasonEntry, string name) {
      return _context.SeasonEntry.Any(_ => _.Season == seasonEntry.Season && _.Team == seasonEntry.Team);
    }

    bool SeasonExists(SeasonEntry seasonEntry, int season) {
      return _context.Season.Any(_ => _.Id == season);
    }
    
    bool TeamExists(SeasonEntry seasonEntry, int team) {
      return _context.Team.Any(_ => _.Id == team);
    }
  }
}