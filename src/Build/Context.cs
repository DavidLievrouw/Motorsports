using Cake.Core;
using Cake.Frosting;
using Motorsports.Build.Properties;

namespace Motorsports.Build {
  public class Context : FrostingContext {
    public Context(ICakeContext context) : base(context) {
      Motorsports = new MotorsportsProperties(context);
    }

    public MotorsportsProperties Motorsports { get; }
  }
}