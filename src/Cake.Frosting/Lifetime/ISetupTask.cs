using Cake.Frosting;

namespace Build.Lifetime {
  public interface ISetupTask {
    void Run(FrostingContext context);
  }
}