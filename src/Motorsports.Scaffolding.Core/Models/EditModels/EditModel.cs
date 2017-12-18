using System;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public abstract class EditModel<TDataModel> where TDataModel : class, new() {
    protected EditModel(TDataModel model) {
      DataModel = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected internal TDataModel DataModel { get; }
  }
}