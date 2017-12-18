using System;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public abstract class DisplayModel<TDataModel> where TDataModel : class, new() {
    protected DisplayModel(TDataModel model) {
      DataModel = model ?? throw new ArgumentNullException(nameof(model));
    }

    protected internal TDataModel DataModel { get; }
  }
}