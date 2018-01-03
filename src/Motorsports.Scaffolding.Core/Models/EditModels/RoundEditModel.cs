using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class RoundEditModel {
    public int Id { get; set; }
    public int Season { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public short Number { get; set; }
    public string Venue { get; set; }

    public int? WinningTeamId { get; set; }
    public IEnumerable<int> WinningParticipantIds { get; set; }

    public RainLevel? Rain { get; set; }
    public RoundStatus? Status { get; set; }
    public short? Rating { get; set; }

    public class RoundEditModelBinder : IModelBinder {
      public Task BindModelAsync(ModelBindingContext bindingContext) {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var request = bindingContext.HttpContext.Request;

        var winningParticipantsStringValues = request.Form[nameof(WinningParticipantIds) + "[]"];
        var idStringStringValues = request.Form[nameof(Id)];
        var seasonStringStringValues = request.Form[nameof(Season)];
        var nameStringStringValues = request.Form[nameof(Name)];
        var dateStringStringValues = request.Form[nameof(Date)];
        var numberStringStringValues = request.Form[nameof(Number)];
        var venueStringStringValues = request.Form[nameof(Venue)];
        var winningTeamIdStringValues = request.Form[nameof(WinningTeamId)];
        var rainStringValues = request.Form[nameof(Rain)];
        var ratingStringValues = request.Form[nameof(Rating)];
        var statusStringValues = request.Form[nameof(Status)];

        var model = new RoundEditModel {
          Id = idStringStringValues == StringValues.Empty
            ? 0
            : idStringStringValues.Select(int.Parse).First(),
          Season = seasonStringStringValues == StringValues.Empty
            ? 0
            : seasonStringStringValues.Select(int.Parse).First(),
          Name = nameStringStringValues == StringValues.Empty
            ? null
            : nameStringStringValues.First(),
          Date = dateStringStringValues == StringValues.Empty
            ? DateTime.MinValue
            : dateStringStringValues.Select(DateTime.Parse).First().Date,
          Number = numberStringStringValues == StringValues.Empty
            ? (short)0
            : numberStringStringValues.Select(short.Parse).First(),
          Venue = venueStringStringValues == StringValues.Empty
            ? null
            : venueStringStringValues.First(),
          WinningTeamId = winningTeamIdStringValues == StringValues.Empty
            ? new int?()
            : winningTeamIdStringValues.Select(int.Parse).First(),
          WinningParticipantIds = winningParticipantsStringValues == StringValues.Empty
            ? Enumerable.Empty<int>()
            : winningParticipantsStringValues.Select(int.Parse),
          Rating = ratingStringValues == StringValues.Empty
            ? new short?()
            : ratingStringValues.Select(short.Parse).First(),
          Rain = rainStringValues == StringValues.Empty
            ? new RainLevel?()
            : rainStringValues.Select(s => Enum.Parse<RainLevel>(s, true)).First(),
          Status = statusStringValues == StringValues.Empty
            ? new RoundStatus?()
            : statusStringValues.Select(s => Enum.Parse<RoundStatus>(s, true)).First()
        };

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
      }
    }
  }
}