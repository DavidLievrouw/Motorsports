using System;
using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Round {
    public Round() {
      RelatedRoundWinners = new HashSet<RoundWinner>();
    }

    public int Id { get; set; }
    public DateTime Date { get; set; }
    public short Number { get; set; }
    public string Name { get; set; }
    public int Season { get; set; }
    public string Venue { get; set; }

    public Season RelatedSeason { get; set; }
    public Venue RelatedVenue { get; set; }
    public RoundResult RelatedRoundResult { get; set; }
    public ICollection<RoundWinner> RelatedRoundWinners { get; set; }
  }
}