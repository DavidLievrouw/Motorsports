using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class HomeDisplayModel {
    public NextUpDisplayModel VeryNextUp { get; set; }
    public bool HasVeryNextUp => VeryNextUp != null;

    public IEnumerable<NextUpDisplayModel> NextUpPerSport { get; set; }
    public bool HasNextUpPerSport => NextUpPerSport.Any();
    
    public IEnumerable<SeasonDisplayModelForHome> LatestSeasons { get; set; }
    public bool HasLatestSeasons => LatestSeasons.Any();

    public class SeasonDisplayModelForHome {
      public SeasonDisplayModelForHome(Season season) {
        DataModel = season ?? throw new ArgumentNullException(nameof(season));
        Logo = "~/img/" + season.Sport + ".png";
      }

      public Season DataModel { get; }

      public int Id {
        get => DataModel.Id;
        set => DataModel.Id = value;
      }

      public string Sport {
        get => DataModel.Sport;
        set => DataModel.Sport = value;
      }

      [DisplayFormat(NullDisplayText = "/")]
      public string Label {
        get => DataModel.Label;
        set => DataModel.Label = value;
      }

      public Sport RelatedSport {
        get => DataModel.RelatedSport;
        set => DataModel.RelatedSport = value;
      }

      [DisplayName("Start date")]
      [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
      public DateTime? StartDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Min();

      [DisplayName("End date")]
      [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
      public DateTime? EndDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Max();
      
      public string Logo { get; set; }
    }
  }
}