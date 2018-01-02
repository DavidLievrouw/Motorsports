using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class Default : FrostingTask<Context> {
    public override void Run(Context context) {
      context.Warning("Please specify a Target to run.");
      base.Run(context);
    }
  }
}