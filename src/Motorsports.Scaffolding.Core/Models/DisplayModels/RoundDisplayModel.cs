using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundDisplayModel {
    public RoundDisplayModel(
      Round round,
      IEnumerable<Season> seasons = null,
      IEnumerable<Team> teams = null,
      IEnumerable<Participant> participants = null,
      IEnumerable<Status> statuses = null,
      IEnumerable<Venue> venues = null) {
      DataModel = round ?? throw new ArgumentNullException(nameof(round));
      AvailableSeasons = seasons;
      AvailableTeams = teams;
      AvailableParticipants = participants;
      AvailableStatuses = statuses;
      AvailableVenues = venues;
      WinningParticipantIds = round.RelatedRoundWinners?.Select(winner => winner.Participant).ToArray() ?? Enumerable.Empty<int>().ToArray();
    }

    public Round DataModel { get; }

    public int Id {
      get => DataModel.Id;
      set => DataModel.Id = value;
    }
    
    [DisplayFormat(NullDisplayText = "/")]
    public string Name {
      get => DataModel.Name;
      set => DataModel.Name = value;
    }
    
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}")]
    public DateTime Date {
      get => DataModel.Date;
      set => DataModel.Date = value;
    }

    public short Number {
      get => DataModel.Number;
      set => DataModel.Number = value;
    }

    public int Season {
      get => DataModel.Season;
      set => DataModel.Season = value;
    }

    public Season RelatedSeason {
      get => DataModel.RelatedSeason;
      set => DataModel.RelatedSeason = value;
    }

    public string Venue {
      get => DataModel.Venue;
      set => DataModel.Venue = value;
    }

    public Venue RelatedVenue {
      get => DataModel.RelatedVenue;
      set => DataModel.RelatedVenue = value;
    }

    public string Status => DataModel.RelatedRoundResult?.Status ?? RoundStatus.Scheduled.ToString();
    
    [DisplayFormat(NullDisplayText = "?")]
    public short? Rating => (short?) DataModel.RelatedRoundResult?.Rating;

    [DisplayFormat(NullDisplayText = "?")]
    public RainLevel? Rain => DataModel.RelatedRoundResult?.Rain.HasValue ?? false
      ? Enum.Parse<RainLevel>(Enum.GetName(typeof(RainLevel), (int)DataModel.RelatedRoundResult.Rain))
      : new RainLevel?();

    public IEnumerable<Team> AvailableTeams { get; }
    public IEnumerable<Participant> AvailableParticipants { get; }
    public IEnumerable<Status> AvailableStatuses { get; }
    public IEnumerable<Season> AvailableSeasons { get; }
    public IEnumerable<Venue> AvailableVenues { get; }
    public IEnumerable<RainLevel> AvailableRainLevels => Enum.GetValues(typeof(RainLevel)).OfType<RainLevel>();

    [DisplayName("Winning team")]
    public int? WinningTeamId => DataModel.RelatedRoundResult?.WinningTeam;

    [DisplayName("Winning participant(s)")]
    public int[] WinningParticipantIds { get; }

    [DisplayName("Winner(s)")]
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