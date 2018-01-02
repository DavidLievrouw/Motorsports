using Cake.Core;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(CreateIISApplication))]
  [Dependency(typeof(RemoveIISApplication))]
  public sealed class CreateIISApplication : FrostingTask {
    public override void Run(ICakeContext context) {
      var props = context.BuildProps<IISApplicationProps>();

      context.CreatePool(props.IISApplicationPoolSettings);
      context.AddSiteApplication(props.IISApplicationSettings); 
    }
  }
}