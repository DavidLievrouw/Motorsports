using Cake.Frosting;
using Cake.IIS;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(RemoveIISApplication))]
  public sealed class RemoveIISApplication : FrostingTask<Context> {
    public override void Run(Context context) {
      if (context.SiteApplicationExists(context.Motorsports.IIS.ScaffoldingIISApplication.ToApplicationSettings()))
        context.RemoveSiteApplication(context.Motorsports.IIS.ScaffoldingIISApplication.ToApplicationSettings());

      if (context.PoolExists(context.Motorsports.IIS.IISApplicationPool.Name)) context.DeletePool(context.Motorsports.IIS.IISApplicationPool.Name);
    }
  }
}