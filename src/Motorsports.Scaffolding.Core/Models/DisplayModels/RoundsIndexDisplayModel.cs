using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundsIndexDisplayModel {
    public Season Season { get; set; }
    public IEnumerable<RoundDisplayModel> Rounds { get; set; }
  }
}