using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public class MotorsportsValidator<TModel, TKey> : AbstractValidator<TModel> {
    const string Key = "Motorsports_KeyForUpdate";

    public async Task<ValidationResult> ValidateForCreateAsync(TModel instance, CancellationToken cancellationToken) {
      var thirdPartyValidationResult = await ValidateAsync(instance, cancellationToken);
      return new ValidationResult(thirdPartyValidationResult);
    }

    public async Task<ValidationResult> ValidateForUpdateAsync(TKey key, TModel instance, CancellationToken cancellationToken = default(CancellationToken)) {
      var validationContext = new ValidationContext<TModel>(instance);
      validationContext.RootContextData.Add(Key, key);
      var thirdPartyValidationResult = await ValidateAsync(validationContext, cancellationToken);
      return new ValidationResult(thirdPartyValidationResult);
    }

    public async Task<ValidationResult> ValidateForDeleteAsync(TModel instance, CancellationToken cancellationToken = default(CancellationToken)) {
      var thirdPartyValidationResult = await ValidateAsync(instance, cancellationToken);
      return new ValidationResult(thirdPartyValidationResult);
    }

    protected TKey GetKey(IValidationContext validationContext) {
      return (TKey) validationContext.RootContextData[Key];
    }
  }
}