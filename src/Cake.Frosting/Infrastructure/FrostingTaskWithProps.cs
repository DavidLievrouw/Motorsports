using System.Reflection;
using Cake.Frosting;

namespace Build.Infrastructure {
  public abstract class FrostingTaskWithProps<TProps> : FrostingTask<FrostingContext> where TProps : TaskProps {
    public override void Run(FrostingContext context) {
      var propsInstance = typeof(TProps).GetConstructor(BindingFlags.Public | BindingFlags.CreateInstance, null, new []{ typeof(FrostingContext) }, null).Invoke(null);
      Props = (TProps) propsInstance;
      base.Run(context);
    }

    public abstract void RunCore(FrostingContext context);

    public TProps Props { get; private set; }
  }
}