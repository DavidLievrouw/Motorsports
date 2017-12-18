using System.ComponentModel;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class SeasonDisplayModel : DisplayModel<Season> {
    public SeasonDisplayModel(Season season) : base(season) { }

    public int Id {
      get => DataModel.Id;
      set => DataModel.Id = value;
    }

    public string Sport {
      get => DataModel.Sport;
      set => DataModel.Sport = value;
    }

    public string Label {
      get => DataModel.Label;
      set => DataModel.Label = value;
    }

    public Sport RelatedSport {
      get => DataModel.RelatedSport;
      set => DataModel.RelatedSport = value;
    }

    [DisplayName("Nice label")]
    public string NiceLabel {
      get {
        if (!string.IsNullOrWhiteSpace(DataModel.Label)) return $"{DataModel.Label} ({DataModel.Sport})";
        var firstRound = DataModel.RelatedRounds?.OrderBy(r => r.Number)?.FirstOrDefault()?.Date;
        var lastRound = DataModel.RelatedRounds?.OrderByDescending(r => r.Number)?.FirstOrDefault()?.Date;
        var firstYear = firstRound?.Year;
        var lastYear = lastRound?.Year;
        if (firstYear.HasValue && lastYear.HasValue) {
          return firstYear.Value == lastYear.Value
            ? $"{DataModel.Sport} ({firstYear.Value})"
            : $"{DataModel.Sport} ({firstYear.Value}-{lastYear.Value})";
        }
        return $"{DataModel.Sport} (no rounds defined)";
      }
    }
  }
}