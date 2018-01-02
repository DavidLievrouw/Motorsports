using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

namespace Build.Tasks {
  public sealed class PublishProps : GlobalProps {
    readonly ICakeContext _context;

    public PublishProps(ICakeContext context) : base(context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      PublishTargetDirectoryPath = RepoRootDirectoryPath + "/pub";
      ScaffoldingProjectFilePath = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";
      
      _context.Information("Publish directory = " + PublishTargetDirectory);
      _context.Information("Scaffolding project file = " + ScaffoldingProjectFile);
    }
    
    public string PublishTargetDirectoryPath { get; set; }
    public string ScaffoldingProjectFilePath { get; set; }

    public DirectoryPath PublishTargetDirectory => _context.MakeAbsolute(_context.Directory(PublishTargetDirectoryPath));
    public FilePath ScaffoldingProjectFile => _context.MakeAbsolute(_context.File(ScaffoldingProjectFilePath));
  }
}