using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Venue {
    public Venue() {
      Round = new HashSet<Round>();
    }

    public string Name { get; set; }
    public string Country { get; set; }

    public Country CountryNavigation { get; set; }
    public ICollection<Round> Round { get; set; }
  }
}