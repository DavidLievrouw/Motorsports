using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

namespace Build.Tasks {
  public sealed class RestorePackagesProps : GlobalProps {
    readonly ICakeContext _context;

    public RestorePackagesProps(ICakeContext context) : base(context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      ScaffoldingProjectFilePath = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";
      _context.Information("Scaffolding project file = " + ScaffoldingProjectFile);
    }
    
    public string ScaffoldingProjectFilePath { get; set; }

    public FilePath ScaffoldingProjectFile => _context.MakeAbsolute(_context.File(ScaffoldingProjectFilePath));
  }
}