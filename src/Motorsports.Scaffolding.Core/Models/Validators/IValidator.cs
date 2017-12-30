using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public interface IValidator<in T> {
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default(CancellationToken));
  }
}