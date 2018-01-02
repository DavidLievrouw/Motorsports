using Cake.Common.Tools.DotNetCore;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(RestorePackages))]
  public sealed class RestorePackages : FrostingTask {
    public override void Run(ICakeContext context) {
      var props = context.GetProps<RestorePackagesProps>();
      context.DotNetCoreRestore(props.ScaffoldingProjectFile.FullPath);
    }
  }
}