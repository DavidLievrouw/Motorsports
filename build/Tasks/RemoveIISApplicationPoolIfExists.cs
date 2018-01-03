using Cake.Core;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(RemoveIISApplicationPoolIfExists))]
  [Dependency(typeof(StopIISApplicationPoolIfExists))]
  public sealed class RemoveIISApplicationPoolIfExists : FrostingTask {
    public override bool ShouldRun(ICakeContext context) {
      var props = context.BuildProps<IISApplicationProps>();
      return context.PoolExists(props.IISApplicationPoolSettings.Name);
    }
    
    public override void Run(ICakeContext context) {
      var props = context.BuildProps<IISApplicationProps>();
      context.DeletePool(props.IISApplicationPoolSettings.Name);
    }
  }
}