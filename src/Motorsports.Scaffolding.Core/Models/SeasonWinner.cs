namespace Motorsports.Scaffolding.Core.Models {
  public partial class SeasonWinner {
    public int Id { get; set; }
    public int Season { get; set; }
    public int Participant { get; set; }

    public Participant ParticipantNavigation { get; set; }
    public Season SeasonNavigation { get; set; }
  }
}