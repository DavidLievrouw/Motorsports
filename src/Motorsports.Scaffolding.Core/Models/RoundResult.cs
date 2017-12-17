namespace Motorsports.Scaffolding.Core.Models {
  public partial class RoundResult {
    public int Round { get; set; }
    public string Status { get; set; }
    public decimal? Rating { get; set; }
    public decimal? Rain { get; set; }
    public int? WinningTeam { get; set; }

    public Round RelatedRound { get; set; }
    public Status RelatedStatus { get; set; }
    public Team RelatedWinningTeam { get; set; }
  }
}