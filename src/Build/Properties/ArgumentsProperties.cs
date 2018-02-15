using System;
using Cake.Common.Tools.DotNetCore;
using Cake.Core;

namespace Motorsports.Build.Properties {
  public class ArgumentsProperties : Properties<ArgumentsProperties> {
    private readonly MotorsportsProperties _container;

    public ArgumentsProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public string Target => Context.Arguments.HasArgument("target") ? Context.Arguments.GetArgument("target") : "Default";
    public string Configuration => Context.Arguments.HasArgument("configuration") ? Context.Arguments.GetArgument("configuration") : "ReleaseWithJs";
    public string PublishEnvironment => Context.Arguments.HasArgument("PublishEnvironment") ? Context.Arguments.GetArgument("PublishEnvironment") : "Production";
    public string PublishDirectory => Context.Arguments.HasArgument("PublishDirectory") ? Context.Arguments.GetArgument("PublishDirectory") : null;

    public DotNetCoreVerbosity DotNetCoreVerbosity {
      get {
        switch (Context.Arguments.GetArgument("verbosity")?.ToLower()) {
          case "quiet":
            return DotNetCoreVerbosity.Quiet;
          case "minimal":
            return DotNetCoreVerbosity.Minimal;
          case "normal":
            return DotNetCoreVerbosity.Normal;
          case "verbose":
            return DotNetCoreVerbosity.Detailed;
          case "diagnostic":
            return DotNetCoreVerbosity.Diagnostic;
          default:
            return DotNetCoreVerbosity.Normal;
        }
      }
    }
  }
}