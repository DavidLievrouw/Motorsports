using System.Collections.Generic;
using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class SeasonEditModel : EditModel<Season> {
    public SeasonEditModel(
      Season season,
      IEnumerable<Team> teams,
      IEnumerable<Participant> participants) : base(season) {
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

    public IEnumerable<Team> Teams { get; }
    public IEnumerable<Participant> Participants { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId {
      get => DataModel.RelatedSeasonResult?.WinningTeam;
      set => DataModel.RelatedSeasonResult = value.HasValue
        ? new SeasonResult {Season = DataModel.Id, WinningTeam = value.Value}
        : null;
    }
  }
}