using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks {
  public abstract class FrostingTaskWithProps<TProps> : FrostingTask<ICakeContext> where TProps : GlobalProps {
    TProps _props;

    public TProps GetProperties(ICakeContext context) {
      return _props ?? (_props = context.GetProps<TProps>());
    }
  }
}