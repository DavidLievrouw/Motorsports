using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Status {
    public Status() {
      RoundResult = new HashSet<RoundResult>();
    }

    public string Name { get; set; }

    public ICollection<RoundResult> RoundResult { get; set; }
  }
}