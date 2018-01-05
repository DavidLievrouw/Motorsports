using System;
using System.ComponentModel.DataAnnotations;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class EventHistoryItemDisplayModel {
    public EventHistoryItemDisplayModel(EventHistoryItem eventHistoryItem) {
      Id = eventHistoryItem.Id;
      Date = eventHistoryItem.Date;
      Rating = eventHistoryItem.Rating;
      Rain = eventHistoryItem.Rain.HasValue
        ? Enum.Parse<RainLevel>(Enum.GetName(typeof(RainLevel), (int) eventHistoryItem.Rain))
        : new RainLevel?();
      WinningTeam = eventHistoryItem.WinningTeam;
      WinningParticipants = eventHistoryItem.WinningParticipants;
    }

    public int Id { get; set; }

    [DisplayFormat(DataFormatString = "{0:ddd, MMM d yyyy}")]
    public DateTime Date { get; set; }

    [DisplayFormat(NullDisplayText = "?", DataFormatString = "{0:0}")]
    public decimal? Rating { get; set; }

    [DisplayFormat(NullDisplayText = "?")]
    public RainLevel? Rain { get; set; }

    [DisplayFormat(NullDisplayText = "?")]
    public string WinningTeam { get; set; }

    [DisplayFormat(NullDisplayText = "?")]
    public string WinningParticipants { get; set; }
  }
}