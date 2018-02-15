using System;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Cake.IIS;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(RecycleIISApplicationPool))]
  public sealed class RecycleIISApplicationPool : FrostingTask<Context> {
    public override bool ShouldRun(Context context) {
      return context.PoolExists(context.Motorsports.IIS.IISApplicationPool.Name);
    }

    public override void Run(Context context) {
      var applicationPoolName = context.Motorsports.IIS.IISApplicationPool.Name;

      context.StopPool(applicationPoolName);
      System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(4)).Wait(); // Make sure it is gracefully stopped
      if (!context.StartPool(applicationPoolName)) {
        context.Error($"Could not start application pool '{applicationPoolName}'.");
      }
    }
  }
}