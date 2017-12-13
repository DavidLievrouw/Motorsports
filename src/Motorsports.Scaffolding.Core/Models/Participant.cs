using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Participant {
    public Participant() {
      RoundWinner = new HashSet<RoundWinner>();
      SeasonWinner = new HashSet<SeasonWinner>();
    }

    public int Id { get; set; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Country { get; set; }

    public Country CountryNavigation { get; set; }
    public ICollection<RoundWinner> RoundWinner { get; set; }
    public ICollection<SeasonWinner> SeasonWinner { get; set; }
  }
}