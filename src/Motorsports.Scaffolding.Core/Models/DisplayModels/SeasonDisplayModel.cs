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
      IEnumerable<SeasonEntry> seasonEntries = null,
      IEnumerable<Participant> participants = null) {
      DataModel = season ?? throw new ArgumentNullException(nameof(season));
      AvailableSports = sports;
      AvailableSeasonEntries = seasonEntries;
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
    public string WinningTeamName => DataModel.RelatedWinningTeam?.RelatedSeasonEntries?.Single(se => se.Season == Id)?.Name ?? DataModel.RelatedWinningTeam?.Name;

    [DisplayName("Start date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? StartDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Min();

    [DisplayName("End date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
    public DateTime? EndDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Max();

    public IEnumerable<SeasonEntry> AvailableSeasonEntries { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Sport> AvailableSports { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId => DataModel.WinningTeam;

    [DisplayName("Winner(s)")]
    public int[] WinningParticipantIds { get; }
  }
}