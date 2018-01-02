using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(RemoveIISApplicationIfExists))]
  public sealed class RemoveIISApplicationIfExists : FrostingTaskWithProps<IISApplicationProps> {
    public override bool ShouldRun(FrostingContext context) {
      var props = GetProperties(context);

      return context.SiteApplicationExists(props.IISApplicationSettings);
    }
    
    public override void Run(FrostingContext context) {
      var props = GetProperties(context);

      context.RemoveSiteApplication(props.IISApplicationSettings); 
    }
  }
}