using Cake.Common.Build;
using Cake.Core;
using Cake.Frosting;

namespace Motorsports.Build {
  public sealed class TaskLifetime : FrostingTaskLifetime<Context> {
    public override void Setup(Context context, ITaskSetupContext info) {
      // Report progress to TeamCity
      if (context.BuildSystem().TeamCity.IsRunningOnTeamCity) {
        context.BuildSystem().TeamCity.WriteStartBlock($"Task: {info.Task.Name}");
      }

      base.Setup(context, info);
    }

    public override void Teardown(Context context, ITaskTeardownContext info) {
      base.Teardown(context, info);
      
      // Report progress to TeamCity
      if (context.BuildSystem().TeamCity.IsRunningOnTeamCity) {
        context.BuildSystem().TeamCity.WriteEndBlock($"Task: {info.Task.Name}");
      }
    }
  }
}