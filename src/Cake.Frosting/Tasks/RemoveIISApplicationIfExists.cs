using Cake.Core;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  [TaskName(nameof(RemoveIISApplicationIfExists))]
  public sealed class RemoveIISApplicationIfExists : FrostingTask {
    public override bool ShouldRun(ICakeContext context) {
      var props = context.GetProps<IISApplicationProps>();
      return context.SiteApplicationExists(props.IISApplicationSettings);
    }
    
    public override void Run(ICakeContext context) {
      var props = context.GetProps<IISApplicationProps>();
      context.RemoveSiteApplication(props.IISApplicationSettings); 
    }
  }
}