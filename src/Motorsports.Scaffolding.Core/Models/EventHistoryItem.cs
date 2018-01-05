using System;

namespace Motorsports.Scaffolding.Core.Models {
  public class EventHistoryItem {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal? Rating { get; set; }
    public decimal? Rain { get; set; } 
    public string WinningTeam { get; set; }
    public string WinningParticipants { get; set; }
  }
}