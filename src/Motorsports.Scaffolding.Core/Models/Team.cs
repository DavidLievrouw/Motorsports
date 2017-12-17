using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Team {
    public Team() {
      RelatedRoundResults = new HashSet<RoundResult>();
      RelatedSeasonResults = new HashSet<SeasonResult>();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Sport { get; set; }
    public string Country { get; set; }

    public Country RelatedCountry { get; set; }
    public Sport RelatedSport { get; set; }
    public ICollection<RoundResult> RelatedRoundResults { get; set; }
    public ICollection<SeasonResult> RelatedSeasonResults { get; set; }
  }
}