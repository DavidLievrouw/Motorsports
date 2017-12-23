using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonDisplayModel : DisplayModel<Season> {
    public SeasonDisplayModel(
      Season season,
      IEnumerable<Sport> sports = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null) : base(season) {
      AvailableSports = sports;
      AvailableTeams = teams;
      AvailableParticipants = participants;
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

    public Sport RelatedSport {
      get => DataModel.RelatedSport;
      set => DataModel.RelatedSport = value;
    }
    
    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Sport> AvailableSports { get; }

    public int? WinningTeamId {
      get => DataModel.RelatedSeasonResult?.WinningTeam;
      set => DataModel.RelatedSeasonResult = value.HasValue
        ? new SeasonResult {Season = DataModel.Id, WinningTeam = value.Value}
        : null;
    }

    [DisplayName("Winning team")]
    public Team WinningTeam => DataModel.RelatedSeasonResult?.RelatedWinningTeam;

    [DisplayName("Nice label")]
    public string NiceLabel {
      get {
        if (!string.IsNullOrWhiteSpace(DataModel.Label)) return $"{DataModel.Label} ({DataModel.Sport})";
        var firstRound = DataModel.RelatedRounds?.OrderBy(r => r.Number)?.FirstOrDefault()?.Date;
        var lastRound = DataModel.RelatedRounds?.OrderByDescending(r => r.Number)?.FirstOrDefault()?.Date;
        var firstYear = firstRound?.Year;
        var lastYear = lastRound?.Year;
        if (firstYear.HasValue && lastYear.HasValue) {
          return firstYear.Value == lastYear.Value
            ? $"{DataModel.Sport} ({firstYear.Value})"
            : $"{DataModel.Sport} ({firstYear.Value}-{lastYear.Value})";
        }
        return $"{DataModel.Sport} (no rounds defined)";
      }
    }
  }
}