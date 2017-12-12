using System.Linq;

// ReSharper disable CheckNamespace

namespace Motorsports.Scaffolding.Web.App_Code {
  public partial class Team {
    public override string ToString() {
      return $"{Sport} - {Name}";
    }
  }
}