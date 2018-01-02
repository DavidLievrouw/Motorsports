using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public interface ITeardownTask {
    void Run(FrostingContext context, ITeardownContext info);
  }
}