using System;
using System.Linq;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public class UpdateParticipantValidator : MotorsportsValidator<Participant, int>, IUpdateValidator<Participant, int> {
    readonly MotorsportsContext _context;

    public UpdateParticipantValidator(MotorsportsContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RuleFor(_ => _.Title)
        .NotEmpty()
        .WithMessage("A title is required.");

      RuleFor(_ => _.FirstName)
        .NotEmpty()
        .WithMessage("A first name is required.");

      RuleFor(_ => _.LastName)
        .NotEmpty()
        .WithMessage("A last name is required.");

      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.");
      
      RuleFor(_ => _.Country)
        .NotEmpty()
        .WithMessage("A country is required.")
        .Must(CountryExists)
        .WithMessage("The specified country does not exist.");

      RuleFor(_ => _.Title)
        .Must(MustExist)
        .WithMessage("The participant to update does not exist.")
        .Must(BeUnique)
        .WithMessage("This participant already exists.")
        .When(_ => !string.IsNullOrEmpty(_.Title) && !string.IsNullOrEmpty(_.FirstName) && !string.IsNullOrEmpty(_.LastName));
    }

    bool CountryExists(Participant participant, string country) {
      return _context.Country.Any(_ => EF.Functions.Like(_.Iso, country));
    }

    bool MustExist(Participant participant, string title) {
      return _context.Participant.Any(_ => participant.Id == _.Id);
    }

    bool BeUnique(Participant participant, string title) {
      return !_context.Participant.Any(_ => 
        EF.Functions.Like(_.Title, participant.Title) && 
        EF.Functions.Like(_.FirstName, participant.FirstName) && 
        EF.Functions.Like(_.LastName, participant.LastName) &&
        _.Id != participant.Id);
    }
  }
}