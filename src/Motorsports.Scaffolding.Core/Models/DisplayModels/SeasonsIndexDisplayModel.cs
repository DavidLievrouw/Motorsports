using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonsIndexDisplayModel {
    public Dictionary<Sport, IEnumerable<SeasonDisplayModel>> SeasonsPerSport { get; set; }
  }
}