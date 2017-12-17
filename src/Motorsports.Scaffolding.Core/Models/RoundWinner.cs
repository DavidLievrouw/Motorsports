namespace Motorsports.Scaffolding.Core.Models {
  public partial class RoundWinner {
    public int Id { get; set; }
    public int Round { get; set; }
    public int Participant { get; set; }

    public Participant RelatedParticipant { get; set; }
    public Round RelatedRound { get; set; }
  }
}