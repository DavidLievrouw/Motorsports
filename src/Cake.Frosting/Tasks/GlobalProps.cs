using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class GlobalProps {
    readonly FrostingContext _context;

    public GlobalProps(FrostingContext context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      ProductName = "Motorsports";
      Target = context.Arguments.GetArgument("target");
      Configuration = context.Arguments.GetArgument("configuration");
      Verbosity = context.Arguments.GetArgument("verbosity");

      RepoRootDirectoryPath = "..";
      SourceDirectoryPath = RepoRootDirectoryPath + "/src";

      _context.Information("Product name = " + ProductName);
      _context.Information("Target = " + Target);
      _context.Information("Configuration = " + Configuration);
      _context.Information("Verbosity = " + Verbosity);
      _context.Information("DotNetCore verbosity = " + DotNetCoreVerbosity);
      _context.Information("Repostory root path = " + RepoRootDirectory);
      _context.Information("Source directory = " + SourceDirectory);
    }

    public string ProductName { get; set; }
    public string Target { get; }
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

    public string RepoRootDirectoryPath { get; set; }
    public string SourceDirectoryPath { get; set; }

    public DirectoryPath RepoRootDirectory => _context.MakeAbsolute(_context.Directory(RepoRootDirectoryPath));
    public DirectoryPath SourceDirectory => _context.MakeAbsolute(_context.Directory(SourceDirectoryPath));
  }
}