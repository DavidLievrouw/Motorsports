using System.Diagnostics.CodeAnalysis;
using Build.Tasks;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

// ReSharper disable once CheckNamespace
public sealed class Lifetime : FrostingLifetime<Context> {
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public static _Props _Props { get; private set; }

  public override void Setup(Context context) {
    context.Information("Setting things up...");
    _Props = new _Props(context);
  }

  public override void Teardown(Context context, ITeardownContext info) {
    context.Information("Tearing things down...");
  }
}