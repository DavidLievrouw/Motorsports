using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class Default : FrostingTask<FrostingContext> {
    public override void Run(FrostingContext context) {
      context.Warning("Please specify a Target to run.");
      base.Run(context);
    }
  }
}