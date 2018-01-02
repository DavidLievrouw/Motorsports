using Cake.Core;
using Cake.Frosting;

// ReSharper disable once CheckNamespace
public class Context : FrostingContext {
  public Context(ICakeContext context)
    : base(context) { }
}