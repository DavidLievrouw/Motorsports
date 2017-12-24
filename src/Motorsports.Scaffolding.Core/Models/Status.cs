using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Status {
    public Status() {
      RelatedRoundResults = new HashSet<RoundResult>();
    }

    public string Name { get; set; }

    public ICollection<RoundResult> RelatedRoundResults { get; set; }
  }
}