using Build.Tasks;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Lifetime {
  public class StopApplicationPoolSetupTask : ISetupTask {
    public void Run(FrostingContext context) {
      if (context.Arguments.GetArgument("target") == nameof(Publish)) {
        context.Information("Stopping application pool...");
        var task = new StopIISApplicationPoolIfExists();
        if (task.ShouldRun(context)) task.Run(context);
      }
    }
  }
}