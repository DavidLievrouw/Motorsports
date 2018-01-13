using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class HomeDisplayModel {
    public NextUpDisplayModel VeryNextUp { get; set; }
    public bool HasVeryNextUp => VeryNextUp != null;

    public IEnumerable<NextUpDisplayModel> NextUp { get; set; }
    public bool HasNextUpPerSport => NextUp.Any();
    
    [DisplayName("Round")]
    public IEnumerable<RoundToAcquireDisplayModel> RoundsToAcquire { get; set; }
    public bool HasRoundsToAcquire => RoundsToAcquire.Any();

    [DisplayName("Season")]
    public IEnumerable<SeasonDisplayModelForHome> LatestSeasons { get; set; }
    public bool HasLatestSeasons => LatestSeasons.Any();

    public class SeasonDisplayModelForHome {
      public SeasonDisplayModelForHome(Season season) {
        DataModel = season ?? throw new ArgumentNullException(nameof(season));
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

      [DisplayName("Start date")]
      [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
      public DateTime? StartDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Min();

      [DisplayName("End date")]
      [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", NullDisplayText = "/")]
      public DateTime? EndDate => DataModel.RelatedRounds?.Select(r => (DateTime?)r.Date).DefaultIfEmpty().Max();
    }
  }
}