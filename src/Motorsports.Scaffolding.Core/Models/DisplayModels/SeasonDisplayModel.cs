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
    
    [DisplayFormat(NullDisplayText = "?")]
    public IEnumerable<Participant> WinningParticipants => DataModel.RelatedSeasonWinners?.Select(sw => sw.RelatedParticipant);
    
    [DisplayFormat(NullDisplayText = "?")]
    public Team WinningTeam => DataModel.RelatedWinningTeam;

    [DisplayName("Start date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? StartDate => DataModel.RelatedRounds?.MinOrDefault(r => r.Date);

    [DisplayName("End date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? EndDate => DataModel.RelatedRounds?.MaxOrDefault(r => r.Date);

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Sport> AvailableSports { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId => DataModel.WinningTeam;

    [DisplayName("Winner(s)")]
    public int[] WinningParticipantIds { get; }
  }
}