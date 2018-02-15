using System;
using Cake.Core;
using Cake.Core.IO;

namespace Motorsports.Build.Properties {
  public class ToolsProperties : Properties<ToolsProperties> {
    private readonly MotorsportsProperties _container;

    public ToolsProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public FilePath NuGetExeTool => Context.Tools.Resolve("NuGet.exe");
  }
}