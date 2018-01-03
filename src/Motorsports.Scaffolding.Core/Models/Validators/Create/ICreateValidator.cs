using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators.Create {
  public interface ICreateValidator<in TModel> {
    Task<ValidationResult> ValidateAsync(TModel instance, CancellationToken cancellationToken = default(CancellationToken));
  }
}