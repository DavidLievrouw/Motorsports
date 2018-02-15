using System;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Motorsports.Build.Tasks {
  public sealed class Default : FrostingTask<Context> {
    public override void Run(Context context) {
      context.Warning("Please specify a Target to run. Exiting...");
      base.Run(context);
    }

    public override void Finally(Context context) {
      base.Finally(context);
      System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5)).Wait(); // Wait for exit
    }
  }
}