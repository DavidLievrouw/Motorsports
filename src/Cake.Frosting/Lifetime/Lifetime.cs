using System;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public sealed class Lifetime : FrostingLifetime<ICakeContext> {
    public override void Setup(ICakeContext context) {
      var setupTasks = GetType().Assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ISetupTask)))
        .ToList();
      context.Information($"Running {setupTasks.Count} setup tasks.");
      foreach (var setupTask in setupTasks) {
        var task = (ISetupTask)Activator.CreateInstance(setupTask);
        context.Information($"Running setup task '{task.Name}'.");
        task.Run(context);
      }
    }

    public override void Teardown(ICakeContext context, ITeardownContext info) {
      var teardownTasks = GetType().Assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ITeardownTask)))
        .ToList();
      context.Information($"Running {teardownTasks.Count} teardown tasks.");
      foreach (var teardownTask in teardownTasks) {
        var task = (ITeardownTask)Activator.CreateInstance(teardownTask);
        context.Information($"Running teardown task '{task.Name}'.");
        task.Run(context, info);
      }
    }
  }
}