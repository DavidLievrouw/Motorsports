using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

// ReSharper disable once CheckNamespace
public sealed class Lifetime : FrostingLifetime<Context> {
  public override void Setup(Context context) {
    context.Information("Setting things up...");
  }

  public override void Teardown(Context context, ITeardownContext info) {
    context.Information("Tearing things down...");
  }
}