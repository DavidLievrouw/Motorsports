using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public class DisallowDeleteValidator
    : IDeleteValidator<Sport>,
      IDeleteValidator<Venue>,
      IDeleteValidator<Team>,
      IDeleteValidator<Participant>,
      IDeleteValidator<Round>,
      IDeleteValidator<Season> {
    public Task<ValidationResult> ValidateForDeleteAsync(Participant instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Id),
            $"Deleting this {nameof(Participant)} is not allowed.")));
    }

    public Task<ValidationResult> ValidateForDeleteAsync(Season instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Id),
            $"Deleting this {nameof(Season)} is not allowed.")));
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

    public Task<ValidationResult> ValidateForDeleteAsync(Round instance, CancellationToken cancellationToken = default(CancellationToken)) {
      return Task.FromResult(
        new ValidationResult(
          new ValidationFailure(
            nameof(instance.Id),
            $"Deleting this {nameof(Round)} is not allowed.")));
    }
  }
}