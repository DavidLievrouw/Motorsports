using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class RestorePackagesProps : TaskProps {
    readonly FrostingContext _context;

    public RestorePackagesProps(FrostingContext context) : base(new GlobalProps(context)) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RepoRootDirectoryPath = "..";
      SourceDirectoryPath = RepoRootDirectoryPath + "/src";
      ScaffoldingProjectFilePath = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";
      
      _context.Information("Repostory root path = " + RepoRootDirectory);
      _context.Information("Source directory = " + SourceDirectory);
      _context.Information("Scaffolding project file = " + ScaffoldingProjectFile);
    }
    
    public string RepoRootDirectoryPath { get; set; }
    public string SourceDirectoryPath { get; set; }
    public string ScaffoldingProjectFilePath { get; set; }

    public DirectoryPath RepoRootDirectory => _context.MakeAbsolute(_context.Directory(RepoRootDirectoryPath));
    public DirectoryPath SourceDirectory => _context.MakeAbsolute(_context.Directory(SourceDirectoryPath));
    public FilePath ScaffoldingProjectFile => _context.MakeAbsolute(_context.File(ScaffoldingProjectFilePath));
  }
}