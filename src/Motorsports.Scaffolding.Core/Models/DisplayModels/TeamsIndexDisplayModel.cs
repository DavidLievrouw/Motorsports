using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class TeamsIndexDisplayModel {
    public Dictionary<Sport, IEnumerable<Team>> TeamsPerSport { get; set; }
  }
}