using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Clean;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Core;
using Cake.Frosting;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(Publish))]
  [Dependency(typeof(RecycleIISApplicationPool))]
  [Dependency(typeof(RestorePackages))]
  [Dependency(typeof(InitVersion))]
  public sealed class Publish : FrostingTask<Context> {
    public override void Run(Context context) {
      var msBuildSettings = new DotNetCoreMSBuildSettings();
      msBuildSettings.Properties.Add("PublishEnvironment", new[] {context.Motorsports.Arguments.PublishEnvironment});

      context.CleanDirectory(context.Motorsports.FileSystem.PublishTargetDirectory);
      context.DotNetCoreClean(
        context.Motorsports.FileSystem.ProjectsAndSolutions.ScaffoldingProjectFile.FullPath,
        new DotNetCoreCleanSettings {
          Verbosity = context.Motorsports.Arguments.DotNetCoreVerbosity,
          Configuration = context.Motorsports.Arguments.Configuration
        });
      context.DotNetCorePublish(
        context.Motorsports.FileSystem.ProjectsAndSolutions.ScaffoldingProjectFile.FullPath,
        new DotNetCorePublishSettings {
          Configuration = context.Motorsports.Arguments.Configuration,
          OutputDirectory = context.Motorsports.FileSystem.ProjectsAndSolutions.ScaffoldingTargetDirectory,
          MSBuildSettings = msBuildSettings,
          Verbosity = context.Motorsports.Arguments.DotNetCoreVerbosity,
          ArgumentCustomization = args => args.Append("--no-restore")
        });
    }
  }
}