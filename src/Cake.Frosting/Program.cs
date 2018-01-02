using Build.Tasks;
using Cake.Frosting;

namespace Build {
  public class Program : IFrostingStartup {
    public void Configure(ICakeServices services) {
      services.UseContext<FrostingContext>();
      services.UseLifetime<Lifetime.Lifetime>();
      services.RegisterInstance(new RestorePackages());
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
}