using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Motorsports.Build {
  public sealed class Lifetime : FrostingLifetime<Context> {
    public override void Setup(Context context) {
      base.Setup(context);

      // Register nuget.exe
      context.Tools.RegisterFile(context.GetAbsoluteFilePath(context.Motorsports.FileSystem.ProjectsAndSolutions.BuildProjectDirectory + "/tools/nuget.exe"));

      // Print out context properties, for debugging purposes
      context.Information(context.Motorsports.ToString());
    }

    public override void Teardown(Context context, ITeardownContext info) {
      base.Teardown(context, info);

      // Report progress to TeamCity
      if (context.BuildSystem().TeamCity.IsRunningOnTeamCity) {
        if (info.ThrownException != null) {
          context.BuildSystem().TeamCity.BuildProblem(info.ThrownException.Message, info.ThrownException.GetType().Name);
        }
      }
    }
  }
}