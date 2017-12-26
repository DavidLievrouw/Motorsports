using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonDisplayModel {
    public SeasonDisplayModel(
      Season season,
      IEnumerable<Sport> sports = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null) {
      DataModel = season ?? throw new ArgumentNullException(nameof(season));
      AvailableSports = sports;
      AvailableTeams = teams;
      AvailableParticipants = participants;
      WinningParticipantIds = season.RelatedSeasonWinners?.Select(winner => winner.Participant).ToArray() ?? Enumerable.Empty<int>().ToArray();
    }

    public Season DataModel { get; }

    public int Id {
      get => DataModel.Id;
      set => DataModel.Id = value;
    }

    public string Sport {
      get => DataModel.Sport;
      set => DataModel.Sport = value;
    }

    [DisplayFormat(NullDisplayText = "/")]
    public string Label {
      get => DataModel.Label;
      set => DataModel.Label = value;
    }

    public Sport RelatedSport {
      get => DataModel.RelatedSport;
      set => DataModel.RelatedSport = value;
    }
    
    [DisplayName("Start date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? StartDate => DataModel.RelatedRounds.FirstOrDefault()?.Date;

    [DisplayName("End date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? EndDate => DataModel.RelatedRounds.LastOrDefault()?.Date;

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Sport> AvailableSports { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId => DataModel.RelatedSeasonResult?.WinningTeam;

    [DisplayName("Winning participant(s)")]
    public int[] WinningParticipantIds { get; }

    [DisplayName("Winner(s)")]
    public string Winners {
      get {
        var winningTeam = DataModel.RelatedSeasonResult?.RelatedWinningTeam;
        var winningParticipants = (DataModel.RelatedSeasonWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        if (!winningParticipants.Any() && winningTeam == null) return "/";
        if (winningParticipants.Any() && winningParticipants.Any() && winningTeam == null) return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))}";
        if (!winningParticipants.Any() && winningTeam != null) return $"{winningTeam.Name}";
        return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))} ({winningTeam?.Name})";
      }
    }

    [DisplayName("Winning team")]
    public string WinningTeam => DataModel.RelatedSeasonResult?.RelatedWinningTeam?.Name ?? "/";

    [DisplayName("Winning participant(s)")]
    public string WinningParticipants {
      get {
        var winningParticipants = (DataModel.RelatedSeasonWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        return winningParticipants.Any()
          ? string.Join(", ", winningParticipants)
          : "/";
      } 
    }
  }
}