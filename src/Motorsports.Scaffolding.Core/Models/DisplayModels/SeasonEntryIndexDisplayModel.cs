using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonEntryIndexDisplayModel {
    public Season Season { get; set; }
    public IEnumerable<SeasonEntry> SeasonEntries { get; set; }
  }
}