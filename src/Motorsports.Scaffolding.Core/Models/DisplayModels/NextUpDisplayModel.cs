using System;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class NextUpDisplayModel {
    public NextUpDisplayModel(NextUp nextUp) {
      Id = nextUp.Id;
      Sport = nextUp.Sport;
      Number = nextUp.Number;
      Date = nextUp.Date;
      Name = nextUp.Name;
      Venue = nextUp.Venue;
    }

    public int Id { get; set; }
    public string Sport { get; set; }
    public short Number { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Venue { get; set; }

    public bool IsInPast => Date.Date <= DateTime.Now.Date;

    public int DaysInFuture => IsInPast
      ? 0
      : (int) (Date.Date - DateTime.Now.Date).TotalDays;
  }
}