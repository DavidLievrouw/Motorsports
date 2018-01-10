using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonEntry {
    public int Season { get; set; }
    public int Team { get; set; }

    [DisplayName("Name during season")]
    public string Name { get; set; }

    [DisplayName("Season")]
    public Season RelatedSeason { get; set; }

    [DisplayName("Team")]
    public Team RelatedTeam { get; set; }
  }
}