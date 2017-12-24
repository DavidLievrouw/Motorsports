using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Venue {
    public Venue() {
      RelatedRounds = new HashSet<Round>();
    }

    public string Name { get; set; }
    public string Country { get; set; }

    public Country RelatedCountry { get; set; }
    public ICollection<Round> RelatedRounds { get; set; }

    public override string ToString() {
      return $"{Name} ({Country})";
    }
  }
}