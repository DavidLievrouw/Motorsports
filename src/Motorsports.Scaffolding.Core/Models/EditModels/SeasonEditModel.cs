using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class SeasonEditModel {
    public int Id { get; set; }
    public string Label { get; set; }
    public int? WinningTeamId { get; set; }
    public IEnumerable<int> WinningParticipantIds { get; set; }

    public class SeasonEditModelBinder : IModelBinder {
      public Task BindModelAsync(ModelBindingContext bindingContext) {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var request = bindingContext.HttpContext.Request;

        var winningParticipantsStringValues = request.Form[nameof(WinningParticipantIds) + "[]"];
        var idStringStringValues = request.Form[nameof(Id)];
        var labelStringStringValues = request.Form[nameof(Label)];
        var winningTeamIdStringValues = request.Form[nameof(WinningTeamId)];

        var model = new SeasonEditModel {
          Id = idStringStringValues == StringValues.Empty
            ? 0
            : idStringStringValues.Select(int.Parse).First(),
          Label = labelStringStringValues == StringValues.Empty
            ? null
            :  string.IsNullOrWhiteSpace(labelStringStringValues.First())
              ? null
              : labelStringStringValues.First(),
          WinningTeamId = winningTeamIdStringValues == StringValues.Empty
            ? new int?()
            : winningTeamIdStringValues.Select(int.Parse).First(),
          WinningParticipantIds = winningParticipantsStringValues == StringValues.Empty
            ? Enumerable.Empty<int>()
            : winningParticipantsStringValues.Select(int.Parse)
        };

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
      }
    }
  }
}