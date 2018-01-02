using System;
using Build.Tasks;
using Cake.Frosting;

// ReSharper disable once CheckNamespace
public class Program : IFrostingStartup {
  public void Configure(ICakeServices services) {
    services.UseContext<FrostingContext>();
    services.UseLifetime<Lifetime>();
    services.RegisterInstance(new RestorePackages());
    services.UseWorkingDirectory("..");
  }

  public static int Main(string[] args) {
    var exitCode = new CakeHostBuilder()
      .WithArguments(args)
      .UseStartup<Program>()
      .Build()
      .Run();

    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));

    return exitCode;
  }
}