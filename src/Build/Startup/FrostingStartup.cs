using Cake.Frosting;

namespace Motorsports.Build.Startup {
  public class FrostingStartup : IFrostingStartup {
    public void Configure(ICakeServices services) {
      services.UseContext<Context>();
      services.UseLifetime<Lifetime>();
      services.UseWorkingDirectory(".");
    }
  }
}