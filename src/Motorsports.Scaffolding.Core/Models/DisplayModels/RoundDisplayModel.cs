using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundDisplayModel {
    readonly Round _round;

    public RoundDisplayModel(
      Round round,
      IEnumerable<Season> seasons = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null,
      IEnumerable<Status> statuses = null) {
      _round = round ?? throw new ArgumentNullException(nameof(round));
      AvailableSeasons = seasons;
      AvailableTeams = teams;
      AvailableParticipants = participants;
      AvailableStatuses = statuses;
      WinningParticipantIds = round.RelatedRoundWinners?.Select(winner => winner.Participant).ToArray() ?? Enumerable.Empty<int>().ToArray();
    }

    public int Id {
      get => _round.Id;
      set => _round.Id = value;
    }

    public string Name {
      get => _round.Name;
      set => _round.Name = value;
    }

    public DateTime Date {
      get => _round.Date;
      set => _round.Date = value;
    }

    public short Number {
      get => _round.Number;
      set => _round.Number = value;
    }

    public int Season {
      get => _round.Season;
      set => _round.Season = value;
    }

    public Season RelatedSeason {
      get => _round.RelatedSeason;
      set => _round.RelatedSeason = value;
    }

    public string Venue {
      get => _round.Venue;
      set => _round.Venue = value;
    }

    public Venue RelatedVenue {
      get => _round.RelatedVenue;
      set => _round.RelatedVenue = value;
    }

    public string Status => _round.RelatedRoundResult?.Status ?? RoundEditModel.RoundStatus.Scheduled.ToString();

    public short? Rating => (short?) _round.RelatedRoundResult?.Rating;

    public RoundEditModel.RainLevel? Rain => _round.RelatedRoundResult?.Rain.HasValue ?? false
      ? Enum.Parse<RoundEditModel.RainLevel>(Enum.GetName(typeof(RoundEditModel.RainLevel), _round.RelatedRoundResult.Rain))
      : new RoundEditModel.RainLevel?();

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Status> AvailableStatuses { get; }
    public IEnumerable<Season> AvailableSeasons { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId {
      get => _round.RelatedRoundResult?.WinningTeam;
      set => _round.RelatedRoundResult = value.HasValue
        ? new RoundResult {Round = _round.Id, WinningTeam = value.Value}
        : null;
    }

    [DisplayName("Winning participant(s)")]
    public int[] WinningParticipantIds { get; set; }

    [DisplayName("Nice name")]
    public string NiceName => $"{_round.RelatedSeason.Sport}: {Number} {RelatedVenue} ({Date:d MMM yyyy}) - {Status}";

    [DisplayName("Winners")]
    public string Winners {
      get {
        var winningTeam = _round.RelatedRoundResult?.RelatedWinningTeam;
        var winningParticipants = (_round.RelatedRoundWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        if (!winningParticipants.Any() && winningTeam == null) return "/";
        if (winningParticipants.Any() && winningParticipants.Any() && winningTeam == null) return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))}";
        if (!winningParticipants.Any() && winningTeam != null) return $"{winningTeam.Name}";
        return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))} ({winningTeam?.Name})";
      }
    }

    [DisplayName("Winning team")]
    public string WinningTeam => _round.RelatedRoundResult?.RelatedWinningTeam?.Name ?? "/";

    [DisplayName("Winning participant(s)")]
    public string WinningParticipants {
      get {
        var winningParticipants = (_round.RelatedRoundWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        return winningParticipants.Any()
          ? string.Join(", ", winningParticipants)
          : "/";
      }
    }
  }
}