using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Core;
using Cake.Frosting;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(RestorePackages))]
  public sealed class RestorePackages : FrostingTask<Context> {
    public override void Run(Context context) {
      context.DotNetCoreRestore(
        context.Motorsports.FileSystem.ProjectsAndSolutions.ScaffoldingProjectFile.FullPath,
        new DotNetCoreRestoreSettings {
          IgnoreFailedSources = true,
          Verbosity = context.Motorsports.Arguments.DotNetCoreVerbosity,
          ArgumentCustomization = args => args.Append("--force")
        });
    }
  }
}