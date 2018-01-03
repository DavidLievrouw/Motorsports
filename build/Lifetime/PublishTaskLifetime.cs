using Build.Tasks;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public class PublishTaskLifetime : IFrostingTaskLifetime {
    public void Setup(ICakeContext context, ITaskSetupContext info) {
      context.Warning("Stopping application pool, if needed");
      var stopPoolTask = new StopIISApplicationPoolIfExists();
      if (stopPoolTask.ShouldRun(context)) stopPoolTask.Run(context);
    }

    public void Teardown(ICakeContext context, ITaskTeardownContext info) {
      context.Information("Starting application pool, if needed...");
      var startPoolTask = new StartIISApplicationPoolIfExists();
      if (startPoolTask.ShouldRun(context)) startPoolTask.Run(context);
    }
  }
}