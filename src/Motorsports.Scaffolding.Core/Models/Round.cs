using System;
using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Round {
    public Round() {
      RoundWinner = new HashSet<RoundWinner>();
    }

    public int Id { get; set; }
    public DateTime Date { get; set; }
    public short Number { get; set; }
    public string Name { get; set; }
    public int Season { get; set; }
    public string Venue { get; set; }

    public Season SeasonNavigation { get; set; }
    public Venue VenueNavigation { get; set; }
    public RoundResult RoundResult { get; set; }
    public ICollection<RoundWinner> RoundWinner { get; set; }
  }
}