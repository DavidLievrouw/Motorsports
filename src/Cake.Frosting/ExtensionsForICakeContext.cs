using System.Reflection;
using Build.Tasks;
using Cake.Core;

namespace Build {
  public static class ExtensionsForICakeContext {
    public static TProps BuildProps<TProps>(this ICakeContext context) where TProps : GlobalProps {
      var propsCtor = typeof(TProps).GetTypeInfo().GetConstructor(new[] {typeof(ICakeContext)});
      return (TProps) propsCtor.Invoke(new [] { context });
    }
  }
}