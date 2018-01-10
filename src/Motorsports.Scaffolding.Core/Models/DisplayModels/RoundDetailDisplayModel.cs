using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundDetailDisplayModel {
    public RoundDetailDisplayModel(RoundDisplayModel round, IEnumerable<EventHistoryItem> eventHistory) {
      Round = round;
      EventHistory = eventHistory
        .Where(eh => eh.Id != round.Id)
        .OrderByDescending(eh => eh.Date)
        .Select(eh => new EventHistoryItemDisplayModel(eh))
        .ToList();
    }

    public RoundDisplayModel Round { get; set; }

    [DisplayName("Event history")]
    public IEnumerable<EventHistoryItemDisplayModel> EventHistory { get; set; }
  }
}