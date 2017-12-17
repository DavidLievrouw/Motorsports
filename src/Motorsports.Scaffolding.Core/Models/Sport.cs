using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Sport {
    public Sport() {
      Season = new HashSet<Season>();
      Team = new HashSet<Team>();
    }

    public string Name { get; set; }

    [DisplayName("Full name")]
    public string FullName { get; set; }

    public ICollection<Season> Season { get; set; }
    public ICollection<Team> Team { get; set; }
  }
}