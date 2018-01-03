using Cake.Core;

namespace Build.Lifetime {
  public interface ISetupTask {
    void Run(ICakeContext context);
    string Name { get; }
  }
}