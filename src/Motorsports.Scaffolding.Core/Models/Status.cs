using System.Collections.Generic;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class Status {
    public Status() {
      RelatedRounds = new HashSet<Round>();
    }

    public string Name { get; set; }

    public ICollection<Round> RelatedRounds { get; set; }
  }
}