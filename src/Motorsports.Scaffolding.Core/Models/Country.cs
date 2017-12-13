using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Country {
    public Country() {
      Participant = new HashSet<Participant>();
      Team = new HashSet<Team>();
      Venue = new HashSet<Venue>();
    }

    public string Iso { get; set; }
    public string Name { get; set; }
    public string NiceName { get; set; }
    public string Iso3 { get; set; }
    public short? NumCode { get; set; }
    public short? PhoneCode { get; set; }

    public ICollection<Participant> Participant { get; set; }
    public ICollection<Team> Team { get; set; }
    public ICollection<Venue> Venue { get; set; }
  }
}