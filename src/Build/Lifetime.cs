using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Motorsports.Build {
  public sealed class Lifetime : FrostingLifetime<Context> {
    public override void Setup(Context context) {
      // Print out context properties, for debugging purposes
      context.Information(context.Motorsports.ToString());
    }

    public override void Teardown(Context context, ITeardownContext info) { }
  }
}