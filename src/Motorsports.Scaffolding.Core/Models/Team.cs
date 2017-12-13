using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Team {
    public Team() {
      RoundResult = new HashSet<RoundResult>();
      SeasonResult = new HashSet<SeasonResult>();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Sport { get; set; }
    public string Country { get; set; }

    public Country CountryNavigation { get; set; }
    public Sport SportNavigation { get; set; }
    public ICollection<RoundResult> RoundResult { get; set; }
    public ICollection<SeasonResult> SeasonResult { get; set; }
  }
}