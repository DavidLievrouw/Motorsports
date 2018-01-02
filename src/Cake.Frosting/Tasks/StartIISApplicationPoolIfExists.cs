using Cake.Core;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(StartIISApplicationPoolIfExists))]
  public sealed class StartIISApplicationPoolIfExists : FrostingTask {
    public override bool ShouldRun(ICakeContext context) {
      var props = context.GetProps<IISApplicationProps>();
      return context.PoolExists(props.IISApplicationPoolSettings.Name);
    }
    
    public override void Run(ICakeContext context) {
      var props = context.GetProps<IISApplicationProps>();
      context.StartPool(props.IISApplicationPoolSettings.Name);
    }
  }
}