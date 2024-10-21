using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateTeamValidator : MotorsportsValidator<Team, int>, IUpdateValidator<Team, int> {
    readonly MotorsportsContext _context;

    public UpdateTeamValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Name)
        .NotEmpty()
        .WithMessage("A name is required.");

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
        .Must(MustExist)
        .WithMessage("The team to update does not exist.")
        .Must(BeUnique)
        .WithMessage("A team with the specified name already exists for this sport.")
        .When(_ => !string.IsNullOrEmpty(_.Name));
    }

    bool MustExist(Team team, string name) {
      return _context.Team.Any(_ => team.Id == _.Id);
    }
    
    bool SportExists(Team team, string sport) {
      return _context.Sport.Any(_ => EF.Functions.Like(_.Name, sport));
    }

    bool CountryExists(Team team, string country) {
      return _context.Country.Any(_ =>  EF.Functions.Like(_.Iso, country));
    }

    bool BeUnique(Team team, string name) {
      return !_context.Team.Any(_ => EF.Functions.Like(_.Name, name) && EF.Functions.Like(_.Sport, team.Sport) && _.Id != team.Id);
    }
  }
}