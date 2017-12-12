using System.Linq;

// ReSharper disable CheckNamespace

namespace Motorsports.Scaffolding.Web.App_Code {
  public partial class Season {
    public string FullName {
      get {
        if (!string.IsNullOrWhiteSpace(Label)) return $"{Sport} ({Label})";
        var firstRound = Rounds?.OrderBy(r => r.Number)?.FirstOrDefault()?.Date;
        var lastRound = Rounds?.OrderByDescending(r => r.Number)?.FirstOrDefault()?.Date;
        var firstYear = firstRound?.Year;
        var lastYear = lastRound?.Year;
        if (firstYear.HasValue && lastYear.HasValue) {
          return firstYear.Value == lastYear.Value
            ? $"{Sport} ({firstYear.Value})"
            : $"{Sport} ({firstYear.Value}-{lastYear.Value})";
        }
        return string.Empty;
      }
    }

    public override string ToString() {
      return FullName;
    }
  }
}