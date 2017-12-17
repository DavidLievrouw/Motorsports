using System.Collections.Generic;
using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Sport {
    public Sport() {
      RelatedSeasons = new HashSet<Season>();
      RelatedTeams = new HashSet<Team>();
    }

    public string Name { get; set; }

    [DisplayName("Full name")]
    public string FullName { get; set; }

    public ICollection<Season> RelatedSeasons { get; set; }
    public ICollection<Team> RelatedTeams { get; set; }
  }
}