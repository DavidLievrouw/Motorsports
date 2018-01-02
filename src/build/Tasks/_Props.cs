using System;
using System.Collections.Generic;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;

namespace Build.Tasks {
  // ReSharper disable once InconsistentNaming
  public sealed class _Props {
    readonly Context _context;

    public _Props(Context context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      ProductName = "Motorsports";
      Configuration = context.Arguments.GetArgument("configuration");
      Verbosity = context.Arguments.GetArgument("verbosity");

      VersionFilePath = "../version.txt";
      SourceDirectoryPath = ".";
      PublishTargetDirectoryPath = "../dist";

      ScaffoldingProjectFilePath = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";
    }

    public string ProductName { get; set; }
    public string Configuration { get; }
    public string Verbosity { get; }

    public DotNetCoreVerbosity DotNetCoreVerbosity {
      get {
        switch (Verbosity.ToLower()) {
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

    public string VersionFilePath { get; set; }
    public string PublishTargetDirectoryPath { get; set; }
    public string SourceDirectoryPath { get; set; }
    public string ScaffoldingProjectFilePath { get; set; }

    public FilePath ScaffoldingProjectFile => _context.MakeAbsolute(_context.File(ScaffoldingProjectFilePath));
    public DirectoryPath PublishTargetDirectory => _context.MakeAbsolute(_context.Directory(PublishTargetDirectoryPath));

    public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

    public T Get<T>(string key) {
      if (key == null) throw new ArgumentNullException(nameof(key));
      if (!Items.TryGetValue(key, out var value)) throw new KeyNotFoundException($"Could not find key '{key}' in {nameof(_Props)} items.");
      return (T) value;
    }

    public bool TryGet<T>(string key, out T value) {
      if (key == null) throw new ArgumentNullException(nameof(key));
      if (!Items.TryGetValue(key, out var valueObj)) {
        value = default(T);
        return false;
      }

      value = (T) valueObj;
      return true;
    }
  }
}