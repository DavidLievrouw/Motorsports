using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Season {
    public Season() {
      Round = new HashSet<Round>();
      SeasonWinner = new HashSet<SeasonWinner>();
    }

    public int Id { get; set; }
    public string Sport { get; set; }
    public string Label { get; set; }

    public Sport SportNavigation { get; set; }
    public SeasonResult SeasonResult { get; set; }
    public ICollection<Round> Round { get; set; }
    public ICollection<SeasonWinner> SeasonWinner { get; set; }
  }
}