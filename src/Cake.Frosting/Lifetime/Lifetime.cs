using System;
using System.Linq;
using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public sealed class Lifetime : FrostingLifetime<FrostingContext> {
    public override void Setup(FrostingContext context) {
      var setupTasks = GetType().Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ISetupTask)));
      foreach (var setupTask in setupTasks) {
        var task = (ISetupTask)Activator.CreateInstance(setupTask);
        task.Run(context);
      }
    }

    public override void Teardown(FrostingContext context, ITeardownContext info) {
      var teardownTasks = GetType().Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ITeardownTask)));
      foreach (var teardownTask in teardownTasks) {
        var task = (ITeardownTask)Activator.CreateInstance(teardownTask);
        task.Run(context, info);
      }
    }
  }
}