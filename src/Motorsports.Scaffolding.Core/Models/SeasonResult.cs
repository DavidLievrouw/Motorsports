namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonResult {
    public int Season { get; set; }
    public int WinningTeam { get; set; }

    public Season SeasonNavigation { get; set; }
    public Team WinningTeamNavigation { get; set; }
  }
}