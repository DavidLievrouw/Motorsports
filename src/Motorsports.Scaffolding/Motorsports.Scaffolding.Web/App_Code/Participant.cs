// ReSharper disable CheckNamespace

namespace Motorsports.Scaffolding.Web.App_Code {
  public partial class Participant {
    public string FullName => $"{FirstName ?? string.Empty} {LastName ?? string.Empty}".Trim();

    public override string ToString() {
      return FullName;
    }
  }
}