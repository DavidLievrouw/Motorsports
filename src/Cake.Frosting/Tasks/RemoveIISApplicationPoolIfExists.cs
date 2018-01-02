using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(RemoveIISApplicationPoolIfExists))]
  [Dependency(typeof(StopIISApplicationPoolIfExists))]
  public sealed class RemoveIISApplicationPoolIfExists : FrostingTaskWithProps<IISApplicationProps> {
    public override bool ShouldRun(FrostingContext context) {
      var props = GetProperties(context);

      return context.PoolExists(props.IISApplicationPoolSettings.Name);
    }
    
    public override void Run(FrostingContext context) {
      var props = GetProperties(context);

      context.DeletePool(props.IISApplicationPoolSettings.Name);
    }
  }
}