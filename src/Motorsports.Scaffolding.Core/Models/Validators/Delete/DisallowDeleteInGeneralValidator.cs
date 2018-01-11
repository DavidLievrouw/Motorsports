using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public class DisallowDeleteInGeneralValidator
    : IDeleteValidator<Sport>,
      IDeleteValidator<Venue>,
      IDeleteValidator<Team>,
      IDeleteValidator<Participant> {
    public Task<ValidationResult> ValidateForDeleteAsync(Participant instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Id),
            $"Deleting this {nameof(Participant)} is not allowed.")));
    }

    public Task<ValidationResult> ValidateForDeleteAsync(Sport instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Name),
            $"Deleting this {nameof(Sport)} is not allowed.")));
    }

    public Task<ValidationResult> ValidateForDeleteAsync(Team instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Id),
            $"Deleting this {nameof(Team)} is not allowed.")));
    }

    public Task<ValidationResult> ValidateForDeleteAsync(Venue instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Name),
            $"Deleting this {nameof(Venue)} is not allowed.")));
    }
  }
}