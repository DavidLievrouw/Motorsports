using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Lifetime {
  public sealed class TaskLifetime : FrostingTaskLifetime<ICakeContext> {
    public override void Setup(ICakeContext context, ITaskSetupContext info) {
      var taskLifetime = GetType().Assembly
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IFrostingTask)))
        .Select(t => t.GetTaskLifetime(info.Task))
        .SingleOrDefault(tl => tl != null);

      if (taskLifetime != null) {
        context.Information($"Running setup for task '{info.Task.Name}'.");
        taskLifetime.Setup(context, info);
      }
    }

    public override void Teardown(ICakeContext context, ITaskTeardownContext info) {
      var taskLifetime = GetType().Assembly
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IFrostingTask)))
        .Select(t => t.GetTaskLifetime(info.Task))
        .SingleOrDefault(tl => tl != null);

      if (taskLifetime != null) {
        context.Information($"Running teardown for {(info.Skipped ? "skipped " : string.Empty)}task '{info.Task.Name}'.");
        taskLifetime.Teardown(context, info);
      }
    }
  }
}