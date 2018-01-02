using Cake.Frosting;

// ReSharper disable once CheckNamespace
public class Program : IFrostingStartup {
  public void Configure(ICakeServices services) {
    services.UseContext<Context>();
    services.UseLifetime<Lifetime>();
    services.UseWorkingDirectory("..");
  }

  public static int Main(string[] args) {
    return new CakeHostBuilder()
      .WithArguments(args)
      .UseStartup<Program>()
      .Build()
      .Run();
  }
}