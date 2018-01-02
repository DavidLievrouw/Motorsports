using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class PublishProps : TaskProps {
    readonly FrostingContext _context;

    public PublishProps(FrostingContext context) : base(new GlobalProps(context)) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      RepoRootDirectoryPath = "..";
      SourceDirectoryPath = RepoRootDirectoryPath + "/src";

      VersionFilePath = RepoRootDirectoryPath + "/version.txt";
      VersionPropsFilePath = RepoRootDirectoryPath + "/version.props";
      ScaffoldingProjectFilePath = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";
      
      _context.Information("Repostory root path = " + RepoRootDirectory);
      _context.Information("Source directory = " + SourceDirectory);
      _context.Information("Publish directory = " + PublishTargetDirectory);
      _context.Information("Scaffolding project file = " + ScaffoldingProjectFile);
      _context.Information("Version file = " + VersionFile);
      _context.Information("Version props file = " + VersionPropsFilePath);
    }
    
    public string VersionFilePath { get; set; }
    public string VersionPropsFilePath { get; set; }
    public string PublishTargetDirectoryPath { get; set; }
    public string ReleaseTargetDirectoryPath { get; set; }
    public string RepoRootDirectoryPath { get; set; }
    public string SourceDirectoryPath { get; set; }
    public string ScaffoldingProjectFilePath { get; set; }

    public DirectoryPath RepoRootDirectory => _context.MakeAbsolute(_context.Directory(RepoRootDirectoryPath));
    public DirectoryPath SourceDirectory => _context.MakeAbsolute(_context.Directory(SourceDirectoryPath));
    public DirectoryPath PublishTargetDirectory => _context.MakeAbsolute(_context.Directory(PublishTargetDirectoryPath));
    public FilePath ScaffoldingProjectFile => _context.MakeAbsolute(_context.File(ScaffoldingProjectFilePath));
    public FilePath VersionFile => _context.MakeAbsolute(_context.File(VersionFilePath));
    public FilePath VersionPropsFile => _context.MakeAbsolute(_context.File(VersionPropsFilePath));
  }
}