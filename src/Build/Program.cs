using System;
using System.Collections.Generic;
using Cake.Core.Configuration;
using Cake.Frosting;
using Cake.NuGet;

namespace Motorsports.Build {
  public class Program : IFrostingStartup {
    public void Configure(ICakeServices services) {
      services.UseContext<Context>();
      services.UseLifetime<Lifetime>();
      services.UseTaskLifetime<TaskLifetime>();
      services.UseWorkingDirectory(".");
      services.UsePackageInstaller<NuGetPackageInstaller>();

      // Workaround, cannot use services.UseModule<NuGetModule>(), because there is no default constructor
      var nuGetModule = new NuGetModule(new CakeConfiguration(new Dictionary<string, string>()));
      nuGetModule.Register(services);
    }

    public static int Main(string[] args) {
      var exitCode = new CakeHostBuilder()
        .WithArguments(args)
        .UseStartup<Program>()
        .Build()
        .Run();
#if DEBUG
      Console.ReadKey();
#endif
      return exitCode;
    }
  }
}