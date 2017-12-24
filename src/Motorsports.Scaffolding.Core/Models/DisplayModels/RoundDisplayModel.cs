using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Motorsports.Scaffolding.Core.Models.EditModels;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundDisplayModel : DisplayModel<Round> {
    public RoundDisplayModel(
      Round round,
      IEnumerable<Season> seasons = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null,
      IEnumerable<Status> statuses = null) : base(round) {
      AvailableSeasons = seasons;
      AvailableTeams = teams;
      AvailableParticipants = participants;
      AvailableStatuses = statuses;
      WinningParticipantIds = round.RelatedRoundWinners?.Select(winner => winner.Participant.ToString()).ToArray() ?? Enumerable.Empty<string>().ToArray();
    }

    public int Id {
      get => DataModel.Id;
      set => DataModel.Id = value;
    }

    public string Name {
      get => DataModel.Name;
      set => DataModel.Name = value;
    }

    public DateTime Date {
      get => DataModel.Date;
      set => DataModel.Date = value;
    }

    public short Number {
      get => DataModel.Number;
      set => DataModel.Number = value;
    }

    public string Status => DataModel.RelatedRoundResult?.Status ?? RoundEditModel.RoundStatus.Scheduled.ToString();

    public short? Rating => (short?) DataModel.RelatedRoundResult?.Rating;

    public RoundEditModel.RainLevel? Rain => DataModel.RelatedRoundResult?.Rain.HasValue ?? false
      ? Enum.Parse<RoundEditModel.RainLevel>(Enum.GetName(typeof(RoundEditModel.RainLevel), DataModel.RelatedRoundResult.Rain))
      : new RoundEditModel.RainLevel?();

    public Season RelatedSeason {
      get => DataModel.RelatedSeason;
      set => DataModel.RelatedSeason = value;
    }

    public Venue RelatedVenue {
      get => DataModel.RelatedVenue;
      set => DataModel.RelatedVenue = value;
    }

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Status> AvailableStatuses { get; }
    public IEnumerable<Season> AvailableSeasons { get; }

    [DisplayName("Winning team")]
    public int? WinningTeamId {
      get => DataModel.RelatedRoundResult?.WinningTeam;
      set => DataModel.RelatedRoundResult = value.HasValue
        ? new RoundResult {Round = DataModel.Id, WinningTeam = value.Value}
        : null;
    }

    [DisplayName("Winning participant(s)")]
    public string[] WinningParticipantIds { get; set; }

    [DisplayName("Nice name")]
    public string NiceName => $"{DataModel.RelatedSeason.Sport}: {Number} {RelatedVenue} ({Date:d MMM yyyy}) - {Status}";

    [DisplayName("Winners")]
    public string Winners {
      get {
        var winningTeam = DataModel.RelatedRoundResult?.RelatedWinningTeam;
        var winningParticipants = (DataModel.RelatedRoundWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        if (!winningParticipants.Any() && winningTeam == null) return "/";
        if (winningParticipants.Any() && winningParticipants.Any() && winningTeam == null) return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))}";
        if (!winningParticipants.Any() && winningTeam != null) return $"{winningTeam.Name}";
        return $"{string.Join(", ", winningParticipants.Select(p => p.GetFullName()))} ({winningTeam?.Name})";
      }
    }

    [DisplayName("Winning team")]
    public string WinningTeam => DataModel.RelatedRoundResult?.RelatedWinningTeam?.Name ?? "/";

    [DisplayName("Winning participant(s)")]
    public string WinningParticipants {
      get {
        var winningParticipants = (DataModel.RelatedRoundWinners?.Select(sw => sw.RelatedParticipant) ?? Enumerable.Empty<Participant>()).ToList();
        return winningParticipants.Any()
          ? string.Join(", ", winningParticipants)
          : "/";
      }
    }
  }
}