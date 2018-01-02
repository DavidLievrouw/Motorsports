using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

// ReSharper disable once CheckNamespace
public sealed class Lifetime : FrostingLifetime<FrostingContext> {
  public override void Setup(FrostingContext context) {
    context.Information("Setting things up...");
  }

  public override void Teardown(FrostingContext context, ITeardownContext info) {
    context.Information("Tearing things down...");
  }
}