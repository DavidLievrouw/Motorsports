using System.Linq;

// ReSharper disable CheckNamespace

namespace Motorsports.Scaffolding.Web.App_Code {
  public partial class Venue {
    public override string ToString() {
      return $"{Name} ({Country})";
    }
  }
}