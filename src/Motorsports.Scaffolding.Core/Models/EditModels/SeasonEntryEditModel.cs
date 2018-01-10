using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class SeasonEntryEditModel {
    public int Season { get; set; }
    public int Team { get; set; }
    public string Name { get; set; }

    public class SeasonEntryEditModelBinder : IModelBinder {
      public Task BindModelAsync(ModelBindingContext bindingContext) {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var request = bindingContext.HttpContext.Request;

        var seasonStringStringValues = request.Form[nameof(Season)];
        var teamStringStringValues = request.Form[nameof(Team)];
        var nameStringValues = request.Form[nameof(Name)];

        var model = new SeasonEntryEditModel {
          Season = seasonStringStringValues == StringValues.Empty
            ? 0
            : seasonStringStringValues.Select(int.Parse).First(),
          Team = teamStringStringValues == StringValues.Empty
            ? 0
            : teamStringStringValues.Select(int.Parse).First(),
          Name = nameStringValues == StringValues.Empty
            ? null
            : string.IsNullOrWhiteSpace(nameStringValues.First())
              ? null
              : nameStringValues.First()
        };

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
      }
    }
  }
}