using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public class MotorsportsValidator<TModel, TKey> : AbstractValidator<TModel> {
    const string Key = "Motorsports_KeyForUpdate";

    public new async Task<ValidationResult> ValidateAsync(TModel instance, CancellationToken cancellationToken) {
      var thirdPartyValidationResult = await base.ValidateAsync(instance, cancellationToken);
      return new ValidationResult(thirdPartyValidationResult);
    }

    public async Task<ValidationResult> ValidateAsync(TKey key, TModel instance, CancellationToken cancellationToken = default(CancellationToken)) {
      var validationContext = new ValidationContext<TModel>(instance);
      validationContext.RootContextData.Add(Key, key);
      var thirdPartyValidationResult = await base.ValidateAsync(validationContext, cancellationToken);
      return new ValidationResult(thirdPartyValidationResult);
    } 
    
    protected TKey GetKey(ValidationContext validationContext) {
      return (TKey) validationContext.RootContextData[Key];
    }
  }
}