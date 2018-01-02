using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Clean;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(Publish))]
  [Dependency(typeof(StopIISApplicationPoolIfExists))]
  [Dependency(typeof(InitVersion))]
  [Dependency(typeof(RestorePackages))]
  public sealed class Publish : FrostingTask {
    public override void Run(ICakeContext context) {
      var props = context.BuildProps<PublishProps>();

      context.CleanDirectory(props.PublishTargetDirectory);
      context.DotNetCoreClean(
        props.ScaffoldingProjectFile.FullPath,
        new DotNetCoreCleanSettings {
          Verbosity = props.DotNetCoreVerbosity,
          Configuration = props.Configuration
        });
      context.DotNetCorePublish(
        props.ScaffoldingProjectFile.FullPath,
        new DotNetCorePublishSettings {
          Configuration = props.Configuration,
          OutputDirectory = props.PublishTargetDirectory,
          Verbosity = props.DotNetCoreVerbosity,
          ArgumentCustomization = args => args.Append("--no-restore")
        });
    }

    public override void Finally(ICakeContext context) {
      base.Finally(context);

      context.Information("Starting application pool, if needed...");
      var startPoolTask = new StartIISApplicationPoolIfExists();
      if (startPoolTask.ShouldRun(context)) startPoolTask.Run(context);
    }
  }
}