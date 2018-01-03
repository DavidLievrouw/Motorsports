using System.Threading;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core.Models.Validators.Update {
  public interface IUpdateValidator<in TModel, in TKey> {
    Task<ValidationResult> ValidateAsync(TKey key, TModel instance, CancellationToken cancellationToken = default(CancellationToken));
  }
}