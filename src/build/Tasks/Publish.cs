using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName("Publish")]
  public sealed class Publish : FrostingTask<Context> {
    public override void Run(Context context) {
      context.Information("Publish!");
    }
  }
}