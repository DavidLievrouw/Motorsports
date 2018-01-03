using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Season {
    public Season() {
      RelatedRounds = new HashSet<Round>();
      RelatedSeasonWinners = new HashSet<SeasonWinner>();
    }

    public int Id { get; set; }
    public string Sport { get; set; }
    public string Label { get; set; }
    public int? WinningTeam { get; set; }

    public Sport RelatedSport { get; set; }
    public Team RelatedWinningTeam { get; set; }
    public ICollection<Round> RelatedRounds { get; set; }
    public ICollection<SeasonWinner> RelatedSeasonWinners { get; set; }
  }
}