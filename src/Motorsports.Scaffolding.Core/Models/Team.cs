using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Team {
    public Team() {
      RelatedRounds = new HashSet<Round>();
      RelatedSeasons = new HashSet<Season>();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Sport { get; set; }
    public string Country { get; set; }

    public Country RelatedCountry { get; set; }
    public Sport RelatedSport { get; set; }
    public ICollection<Round> RelatedRounds { get; set; }
    public ICollection<Season> RelatedSeasons { get; set; }
  }
}