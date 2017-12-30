using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public class MotorsportsValidator<T> : AbstractValidator<T>, IValidator<T> {
    public new async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken) {
      return new ValidationResult(await base.ValidateAsync(instance, cancellationToken));
    }
  }
}