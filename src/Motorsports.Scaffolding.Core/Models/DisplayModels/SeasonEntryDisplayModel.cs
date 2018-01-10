using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonEntryDisplayModel {
    public SeasonEntryDisplayModel(
      SeasonEntry seasonEntry,
      IEnumerable<Team> teams = null) {
      DataModel = seasonEntry ?? throw new ArgumentNullException(nameof(seasonEntry));
      AvailableTeams = teams;
    }

    public SeasonEntry DataModel { get; }

    public int Season => DataModel.Season;
    public int Team => DataModel.Team;

    [DisplayName("Name during season")]
    public string Name => DataModel.Name;

    [DisplayName("Season")]
    public Season RelatedSeason => DataModel.RelatedSeason;

    [DisplayName("Team")]
    public Team RelatedTeam => DataModel.RelatedTeam;

    public IEnumerable<Team> AvailableTeams { get; }
  }
}