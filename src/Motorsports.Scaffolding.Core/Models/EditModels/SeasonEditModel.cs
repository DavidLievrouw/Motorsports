using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class SeasonEditModel : EditModel<Season> {
    public SeasonEditModel(
      Season season, 
      IEnumerable<Sport> sports,
      IEnumerable<Team> teams,
      IEnumerable<Participant> participants) : base(season) {
      Sports = sports;
      Teams = teams;
      Participants = participants;
    }

    public int Id {
      get => DataModel.Id;
      set => DataModel.Id = value;
    }

    public string Sport {
      get => DataModel.Sport;
      set => DataModel.Sport = value;
    }

    public string Label {
      get => DataModel.Label;
      set => DataModel.Label = value;
    }

    public IEnumerable<Sport> Sports { get; }
    public IEnumerable<Team> Teams { get; }
    public IEnumerable<Participant> Participants { get; }

    public IEnumerable<Participant> WinningParticipants { get; set; }
    public Team WinningTeam { get; set; }
  }
}