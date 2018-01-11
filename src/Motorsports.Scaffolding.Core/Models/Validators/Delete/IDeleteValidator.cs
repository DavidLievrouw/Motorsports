using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators.Delete {
  public interface IDeleteValidator<in TModel> {
    Task<ValidationResult> ValidateForDeleteAsync(TModel instance, CancellationToken cancellationToken = default(CancellationToken));
  }
}