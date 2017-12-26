namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonResult {
    public int Season { get; set; }
    public int? WinningTeam { get; set; }

    public Season RelatedSeason { get; set; }
    public Team RelatedWinningTeam { get; set; }
  }
}