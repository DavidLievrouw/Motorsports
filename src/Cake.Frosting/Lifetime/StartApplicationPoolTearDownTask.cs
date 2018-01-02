using Build.Tasks;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public class StartApplicationPoolTeardownTask : ITeardownTask {
    public void Run(FrostingContext context, ITeardownContext info) {
      if (context.Arguments.GetArgument("target") == nameof(Publish)) {
        context.Information("Starting application pool...");
        var task = new StartIISApplicationPoolIfExists();
        if (task.ShouldRun(context)) task.Run(context);
      }
    }
  }
}