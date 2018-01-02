using System;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(StopIISApplicationPoolIfExists))]
  public sealed class StopIISApplicationPoolIfExists : FrostingTaskWithProps<IISApplicationProps> {
    public override bool ShouldRun(ICakeContext context) {
      var props = GetProperties(context);
      return context.PoolExists(props.IISApplicationPoolSettings.Name);
    }
    
    public override void Run(ICakeContext context) {
      var props = GetProperties(context);
      context.StopPool(props.IISApplicationPoolSettings.Name);
    }

    public override void Finally(ICakeContext context) {
      base.Finally(context);
      context.Information("Waiting for application pool to gracefully quit...");
      System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(4)).Wait(); // Make sure it is stopped
    }
  }
}