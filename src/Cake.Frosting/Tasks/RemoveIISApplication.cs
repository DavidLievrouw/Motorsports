using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(RemoveIISApplication))]
  [Dependency(typeof(StopIISApplicationPoolIfExists))]
  [Dependency(typeof(RemoveIISApplicationIfExists))]
  [Dependency(typeof(RemoveIISApplicationPoolIfExists))]
  public sealed class RemoveIISApplication : FrostingTaskWithProps<IISApplicationProps> { }
}