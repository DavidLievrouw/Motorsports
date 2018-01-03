using System;

namespace Build.Lifetime {
  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public sealed class TaskLifetimeAttribute : Attribute {
    public TaskLifetimeAttribute(Type type) {
      Type = type;
    }

    public Type Type { get; }
  }
}