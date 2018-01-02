using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(CreateIISApplication))]
  [Dependency(typeof(RemoveIISApplication))]
  public sealed class CreateIISApplication : FrostingTaskWithProps<IISApplicationProps> {
    public override void Run(FrostingContext context) {
      var props = GetProperties(context);

      context.CreatePool(props.IISApplicationPoolSettings);
      context.AddSiteApplication(props.IISApplicationSettings); 
    }
  }
}