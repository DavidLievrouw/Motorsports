using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Motorsports.Build {
  public sealed class Lifetime : FrostingLifetime<Context> {
    public override void Setup(Context context) {
      base.Setup(context);

      // Print out context properties, for debugging purposes
      context.Information(context.Motorsports.ToString());
    }
  }
}