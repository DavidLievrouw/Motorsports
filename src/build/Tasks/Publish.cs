using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Clean;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(Publish))]
  [Dependency(typeof(InitVersion))]
  public sealed class Publish : FrostingTask<Context> {
    public override void Run(Context context) {
      context.CleanDirectory(Lifetime._Props.PublishTargetDirectory);
      context.DotNetCoreRestore(Lifetime._Props.ScaffoldingProjectFile.FullPath);
      context.DotNetCoreClean(
        Lifetime._Props.ScaffoldingProjectFile.FullPath,
        new DotNetCoreCleanSettings {
          Verbosity = Lifetime._Props.DotNetCoreVerbosity,
          Configuration = Lifetime._Props.Configuration
        });
      context.DotNetCorePublish(
        Lifetime._Props.ScaffoldingProjectFile.FullPath,
        new DotNetCorePublishSettings {
          Configuration = Lifetime._Props.Configuration,
          OutputDirectory = Lifetime._Props.PublishTargetDirectory,
          Verbosity = Lifetime._Props.DotNetCoreVerbosity,
          ArgumentCustomization = args => args.Append("--no-restore")
        });
    }
  }
}