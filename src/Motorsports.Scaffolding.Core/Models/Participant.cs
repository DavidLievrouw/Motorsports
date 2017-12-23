using System.Collections.Generic;
using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Participant {
    public Participant() {
      WonRounds = new HashSet<RoundWinner>();
      WonSeasons = new HashSet<SeasonWinner>();
    }

    public int Id { get; set; }

    public string Title { get; set; }

    [DisplayName("First name")]
    public string FirstName { get; set; }

    [DisplayName("Last name")]
    public string LastName { get; set; }

    public string Country { get; set; }

    public Country RelatedCountry { get; set; }

    public ICollection<RoundWinner> WonRounds { get; set; }
    public ICollection<SeasonWinner> WonSeasons { get; set; }

    public string GetFullName() {
      return $"{FirstName} {LastName}";
    }

    public override string ToString() {
      return $"{GetFullName()} ({Country})";
    }
  }
}