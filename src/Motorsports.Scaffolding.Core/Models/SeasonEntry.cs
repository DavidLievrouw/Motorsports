namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonEntry {
    public int Season { get; set; }
    public int Team { get; set; }
    public string Name { get; set; }

    public Season RelatedSeason { get; set; }
    public Team RelatedTeam { get; set; }
  }
}