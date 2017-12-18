namespace Motorsports.Scaffolding.Core.Models.EditModels {
  public class SeasonEditModel : EditModel<Season> {
    public SeasonEditModel(Season season) : base(season) { }

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
  }
}