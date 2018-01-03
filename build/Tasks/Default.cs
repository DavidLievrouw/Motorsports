using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class Default : FrostingTask {
    public override void Run(ICakeContext context) {
      context.Warning("Please specify a Target to run.");
      base.Run(context);
    }
  }
}