namespace Motorsports.Scaffolding.Core.Models.UpdateModels {
  public class SeasonUpdateModel {
    public int Id { get; set; }
    public string Label { get; set; }
    public int? WinningTeamId { get; set; }
  }
}