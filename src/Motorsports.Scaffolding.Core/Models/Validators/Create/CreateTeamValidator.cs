using System;
using System.Linq;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public class CreateTeamValidator : MotorsportsValidator<Team, int>, ICreateValidator<Team> {
    readonly MotorsportsContext _context;

    public CreateTeamValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.");
      
      RuleFor(_ => _.Sport)
        .NotEmpty()
        .WithMessage("A sport is required.")
        .Must(SportExists)
        .WithMessage("The specified sport does not exist.");
      
      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.")
        .Must(CountryExists)
        .WithMessage("The specified country does not exist.");

      RuleFor(_ => _.Name)
        .Must(BeUnique)
        .WithMessage("A team with the specified name already exists for this sport.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool SportExists(Team team, string sport) {
      return _context.Sport.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, sport));
    }
    
    bool CountryExists(Team team, string country) {
      return _context.Country.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Iso, country));
    }

    bool BeUnique(Team team, string name) {
      return !_context.Team.Any(_ => StringComparer.InvariantCultureIgnoreCase.Equals(_.Name, name) && StringComparer.InvariantCultureIgnoreCase.Equals(_.Sport, team.Sport));
    }
  }
}