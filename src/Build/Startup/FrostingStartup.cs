using Cake.Frosting;
using Microsoft.Extensions.DependencyInjection;

namespace Motorsports.Build.Startup {
  public class FrostingStartup : IFrostingStartup {
    public void Configure(IServiceCollection services) {
      services.UseContext<Context>();
      services.UseLifetime<Lifetime>();
      services.UseWorkingDirectory(".");
    }
  }
}