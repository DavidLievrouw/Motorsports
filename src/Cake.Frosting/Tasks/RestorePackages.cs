using Cake.Common.Tools.DotNetCore;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(RestorePackages))]
  public sealed class RestorePackages : FrostingTaskWithProps<RestorePackagesProps> {
    public override void Run(ICakeContext context) {
      var props = GetProperties(context);

      context.DotNetCoreRestore(props.ScaffoldingProjectFile.FullPath);
    }
  }
}