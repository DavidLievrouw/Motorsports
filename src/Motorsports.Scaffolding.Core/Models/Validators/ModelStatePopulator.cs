using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Motorsports.Scaffolding.Core.Models.Validators.Create;
using Motorsports.Scaffolding.Core.Models.Validators.Delete;
using Motorsports.Scaffolding.Core.Models.Validators.Update;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public interface IModelStatePopulator<in TModel, in TKey> {
    Task ValidateAndPopulateForCreate(ModelStateDictionary modelState, TModel instance);
    Task ValidateAndPopulateForDelete(ModelStateDictionary modelState, TModel instance);
    Task ValidateAndPopulateForUpdate(ModelStateDictionary modelState, TKey key, TModel instance);
  }

  public class ModelStatePopulator<TModel, TKey> : IModelStatePopulator<TModel, TKey> {
    readonly ICreateValidator<TModel> _createValidator;
    readonly IUpdateValidator<TModel, TKey> _updateValidator;
    readonly IDeleteValidator<TModel> _deleteValidator;

    public ModelStatePopulator(ICreateValidator<TModel> createValidator, IUpdateValidator<TModel, TKey> updateValidator, IDeleteValidator<TModel> deleteValidator) {
      _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
      _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
      _deleteValidator = deleteValidator ?? throw new ArgumentNullException(nameof(deleteValidator));
    }

    public async Task ValidateAndPopulateForCreate(ModelStateDictionary modelState, TModel instance) {
      if (modelState == null) throw new ArgumentNullException(nameof(modelState));
      ProcessResult(modelState, await _createValidator.ValidateForCreateAsync(instance));
    }

    public async Task ValidateAndPopulateForDelete(ModelStateDictionary modelState, TModel instance) {
      if (modelState == null) throw new ArgumentNullException(nameof(modelState));
      ProcessResult(modelState, await _deleteValidator.ValidateForDeleteAsync(instance));
    }

    public async Task ValidateAndPopulateForUpdate(ModelStateDictionary modelState, TKey key, TModel instance) {
      if (modelState == null) throw new ArgumentNullException(nameof(modelState));
      ProcessResult(modelState, await _updateValidator.ValidateForUpdateAsync(key, instance));
    }

    static void ProcessResult(ModelStateDictionary modelState, ValidationResult validationResult) {
      if (validationResult.IsValid) return;
      foreach (var validationError in validationResult.Errors) {
        modelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
      }
    }
  }
}