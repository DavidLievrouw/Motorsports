using System;
using System.Linq;
using System.Reflection;
using Build.Lifetime;
using Cake.Core;
using Cake.Frosting;

namespace Build {
  public static class ExtensionsForType {
    public static string GetTaskName(this Type task) {
      var attribute = task.GetTypeInfo().GetCustomAttribute<TaskNameAttribute>();
      return attribute != null
        ? attribute.Name
        : task.Name;
    }

    public static IFrostingTaskLifetime GetTaskLifetime(this Type task, ICakeTaskInfo cakeTaskInfo) {
      var attribute = task
        .GetTypeInfo()
        .GetCustomAttributes<TaskLifetimeAttribute>()
        .SingleOrDefault(attrib => attrib != null && task.GetTaskName() == cakeTaskInfo.Name);
      return attribute != null
        ? (IFrostingTaskLifetime)Activator.CreateInstance(attribute.Type)
        : null;
    }
  }
}