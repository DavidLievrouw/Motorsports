using Cake.Core;

namespace Build.Lifetime {
  public interface ITeardownTask {
    void Run(ICakeContext context, ITeardownContext info);
    string Name { get; }
  }
}