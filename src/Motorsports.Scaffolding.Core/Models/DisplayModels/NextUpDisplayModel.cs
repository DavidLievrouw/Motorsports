using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        .OrderByDescending(eh => eh.Date)
        .Select(eh => new EventHistoryItemDisplayModel(eh))
        .ToList();
    }
    
    public int Id { get; set; }
    public string Sport { get; set; }
    public short Number { get; set; }
    public string Venue { get; set; }
    public bool IsVeryNextUp { get; set; }
    public IEnumerable<EventHistoryItemDisplayModel> EventHistory { get; set; }
    
    [DisplayFormat(NullDisplayText = "/")]
    public string Name { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}")]
    public DateTime Date { get; set; }

    public bool IsInPast => Date.Date <= DateTime.Now.Date;

    public int DaysInFuture => IsInPast
      ? 0
      : (int) (Date.Date - DateTime.Now.Date).TotalDays;
  }
}