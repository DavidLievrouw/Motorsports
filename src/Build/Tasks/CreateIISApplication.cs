using Cake.Frosting;
using Cake.IIS;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(CreateIISApplication))]
  [Dependency(typeof(RemoveIISApplication))]
  public sealed class CreateIISApplication : FrostingTask<Context> {
    public override void Run(Context context) {
      context.CreatePool(context.Motorsports.IIS.IISApplicationPool.ToApplicationPoolSettings());
      context.AddSiteApplication(context.Motorsports.IIS.ScaffoldingIISApplication.ToApplicationSettings());
    }
  }
}