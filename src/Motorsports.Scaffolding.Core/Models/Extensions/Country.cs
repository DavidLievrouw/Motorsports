namespace Motorsports.Scaffolding.Core.Models {
  public partial class Country {
    public override string ToString() {
      return $"{NiceName} ({Iso})";
    }
  }
}