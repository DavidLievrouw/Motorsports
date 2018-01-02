using Cake.Core;
using Cake.Frosting;

namespace Build.Infrastructure {
  public abstract class FrostingTaskWithProps<TProps> : FrostingTask<FrostingContext> where TProps : TaskProps {
    public TProps GetProperties(ICakeContext context) {
      return  (TProps) typeof(TProps).GetConstructor(new[] {typeof(FrostingContext)}).Invoke(new [] { context });
    }
  }
}