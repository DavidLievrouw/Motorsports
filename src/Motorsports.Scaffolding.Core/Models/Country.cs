using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Country {
    public Country() {
      RelatedParticipants = new HashSet<Participant>();
      RelatedTeams = new HashSet<Team>();
      RelatedVenues = new HashSet<Venue>();
    }

    public string Iso { get; set; }
    public string Name { get; set; }
    public string NiceName { get; set; }
    public string Iso3 { get; set; }
    public short? NumCode { get; set; }
    public short? PhoneCode { get; set; }

    public ICollection<Participant> RelatedParticipants { get; set; }
    public ICollection<Team> RelatedTeams { get; set; }
    public ICollection<Venue> RelatedVenues { get; set; }
  }
}