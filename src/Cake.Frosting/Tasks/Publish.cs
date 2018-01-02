using Build.Infrastructure;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Clean;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(Publish))]
  [Dependency(typeof(InitVersion))]
  public sealed class Publish : FrostingTaskWithProps<PublishProps> {
    public override void RunCore(FrostingContext context) {
      context.CleanDirectory(Props.PublishTargetDirectory);
      context.DotNetCoreRestore(Props.ScaffoldingProjectFile.FullPath);
      context.DotNetCoreClean(
        Props.ScaffoldingProjectFile.FullPath,
        new DotNetCoreCleanSettings {
          Verbosity = Props.GlobalProps.DotNetCoreVerbosity,
          Configuration = Props.GlobalProps.Configuration
        });
      context.DotNetCorePublish(
        Props.ScaffoldingProjectFile.FullPath,
        new DotNetCorePublishSettings {
          Configuration = Props.GlobalProps.Configuration,
          OutputDirectory = Props.PublishTargetDirectory,
          Verbosity = Props.GlobalProps.DotNetCoreVerbosity,
          ArgumentCustomization = args => args.Append("--no-restore")
        });
    }
  }
}