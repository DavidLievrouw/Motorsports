using System;
using System.Collections.Generic;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class NextUpDisplayModel {
    public NextUpDisplayModel(
      NextUp nextUp, 
      IEnumerable<NextUp> allRoundsNextUp, 
      IEnumerable<EventHistoryItem> eventHistory) {
      Id = nextUp.Id;
      Sport = nextUp.Sport;
      Number = nextUp.Number;
      Date = nextUp.Date;
      Name = nextUp.Name;
      Venue = nextUp.Venue;
      IsVeryNextUp = allRoundsNextUp
                       .Where(n => n.Date.Date <= DateTime.Now.Date)
                       .OrderBy(n => n.Date)
                       .FirstOrDefault() == nextUp;
      EventHistory = eventHistory
        .Where(eh => eh.Id != nextUp.Id)
        .Select(eh => new EventHistoryItemDisplayModel(eh))
        .ToList();
      Logo = "~/img/" + nextUp.Sport + ".png";
    }
    
    public int Id { get; set; }
    public string Sport { get; set; }
    public short Number { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Venue { get; set; }
    public bool IsVeryNextUp { get; set; }
    public IEnumerable<EventHistoryItemDisplayModel> EventHistory { get; set; }
    public string Logo { get; set; }

    public bool IsInPast => Date.Date <= DateTime.Now.Date;

    public int DaysInFuture => IsInPast
      ? 0
      : (int) (Date.Date - DateTime.Now.Date).TotalDays;
  }
}