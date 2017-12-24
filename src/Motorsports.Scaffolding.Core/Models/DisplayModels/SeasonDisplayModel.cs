using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonDisplayModel {
    readonly Season _season;

    public SeasonDisplayModel(
      Season season,
      IEnumerable<Sport> sports = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null) {
      _season = season ?? throw new ArgumentNullException(nameof(season));
      AvailableSports = sports;
      AvailableTeams = teams;
      AvailableParticipants = participants;
      WinningParticipantIds = season.RelatedSeasonWinners?.Select(winner => winner.Participant).ToArray() ?? Enumerable.Empty<int>().ToArray();
    }

    public int Id {
      get => _season.Id;
      set => _season.Id = value;
    }

    public string Sport {
      get => _season.Sport;
      set => _season.Sport = value;
    }

    public string Label {
      get => _season.Label;
      set => _season.Label = value;
    }

    public Sport RelatedSport {
      get => _season.RelatedSport;
      set => _season.RelatedSport = value;
    }

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Sport> AvailableSports { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId {
      get => _season.RelatedSeasonResult?.WinningTeam;
      set => _season.RelatedSeasonResult = value.HasValue
        ? new SeasonResult {Season = _season.Id, WinningTeam = value.Value}
        : null;
    }
    
    [DisplayName("Winning participant(s)")]
    public int[] WinningParticipantIds { get; set; }

    [DisplayName("Nice label")]
    public string NiceLabel {
      get {
        if (!string.IsNullOrWhiteSpace(_season.Label)) return $"{_season.Label} ({_season.Sport})";
        var firstRound = _season.RelatedRounds?.OrderBy(r => r.Number)?.FirstOrDefault()?.Date;
        var lastRound = _season.RelatedRounds?.OrderByDescending(r => r.Number)?.FirstOrDefault()?.Date;
        var firstYear = firstRound?.Year;
        var lastYear = lastRound?.Year;
        if (firstYear.HasValue && lastYear.HasValue) {
          return firstYear.Value == lastYear.Value
            ? $"{_season.Sport} ({firstYear.Value})"
            : $"{_season.Sport} ({firstYear.Value}-{lastYear.Value})";
        }
        return $"{_season.Sport} (no rounds defined)";
      }
    }

    [DisplayName("Winners")]
    public string Winners {
      get {
        var winningTeam = _season.RelatedSeasonResult?.RelatedWinningTeam;
        var winningParticipants = (_season.RelatedSeasonWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        if (!winningParticipants.Any() && winningTeam == null) return "/";
        if (winningParticipants.Any() && winningParticipants.Any() && winningTeam == null) return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))}";
        if (!winningParticipants.Any() && winningTeam != null) return $"{winningTeam.Name}";
        return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))} ({winningTeam?.Name})";
      }
    }

    [DisplayName("Winning team")]
    public string WinningTeam => _season.RelatedSeasonResult?.RelatedWinningTeam?.Name ?? "/";

    [DisplayName("Winning participant(s)")]
    public string WinningParticipants {
      get {
        var winningParticipants = (_season.RelatedSeasonWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        return winningParticipants.Any()
          ? string.Join(", ", winningParticipants)
          : "/";
      } 
    }
  }
}