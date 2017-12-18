using System.ComponentModel;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Season {
    [DisplayName("Nice label")]
    public string NiceLabel {
      get {
        if (!string.IsNullOrWhiteSpace(Label)) return $"{Label} ({Sport})";
        var firstRound = RelatedRounds?.OrderBy(r => r.Number)?.FirstOrDefault()?.Date;
        var lastRound = RelatedRounds?.OrderByDescending(r => r.Number)?.FirstOrDefault()?.Date;
        var firstYear = firstRound?.Year;
        var lastYear = lastRound?.Year;
        if (firstYear.HasValue && lastYear.HasValue) {
          return firstYear.Value == lastYear.Value
            ? $"{Sport} ({firstYear.Value})"
            : $"{Sport} ({firstYear.Value}-{lastYear.Value})";
        }
        return $"{Sport} (no rounds defined)";
      }
    }
  }
}