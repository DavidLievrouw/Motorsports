namespace Motorsports.Scaffolding.Core.Models {
  public partial class RoundWinner {
    public int Id { get; set; }
    public int Round { get; set; }
    public int Participant { get; set; }

    public Participant ParticipantNavigation { get; set; }
    public Round RoundNavigation { get; set; }
  }
}