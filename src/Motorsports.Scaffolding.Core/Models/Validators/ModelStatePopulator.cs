using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Motorsports.Scaffolding.Core.Models.Validators.Create;
using Motorsports.Scaffolding.Core.Models.Validators.Update;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public interface IModelStatePopulator<in T> {
    Task ValidateAndPopulateForCreate(ModelStateDictionary modelState, T instance);
    Task ValidateAndPopulateForUpdate(ModelStateDictionary modelState, T instance);
  }

  public class ModelStatePopulator<T> : IModelStatePopulator<T> {
    readonly ICreateValidator<T> _createValidator;
    readonly IUpdateValidator<T> _updateValidator;

    public ModelStatePopulator(ICreateValidator<T> createValidator, IUpdateValidator<T> updateValidator) {
      _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
      _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    public Task ValidateAndPopulateForCreate(ModelStateDictionary modelState, T instance) {
      if (modelState == null) throw new ArgumentNullException(nameof(modelState));
      return Populate(modelState, instance, _createValidator);
    }
    
    public Task ValidateAndPopulateForUpdate(ModelStateDictionary modelState, T instance) {
      if (modelState == null) throw new ArgumentNullException(nameof(modelState));
      return Populate(modelState, instance, _updateValidator);
    }

    static async Task Populate(ModelStateDictionary modelState, T instance, IValidator<T> validator) {
      var validationResult = await validator.ValidateAsync(instance);
      if (!validationResult.IsValid) {
        foreach (var validationError in validationResult.Errors) {
          modelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
        }
      }
    }
  }
}