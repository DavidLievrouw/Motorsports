using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

namespace Build.Tasks {
  public sealed class InitVersionProps : GlobalProps {
    readonly ICakeContext _context;

    public InitVersionProps(ICakeContext context) : base(context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      VersionFilePath = RepoRootDirectoryPath + "/version.txt";
      VersionPropsFilePath = SourceDirectoryPath + "/version.props";
      
      _context.Information("Version file = " + VersionFile);
      _context.Information("Version props file = " + VersionPropsFilePath);
    }
    
    public string VersionFilePath { get; set; }
    public string VersionPropsFilePath { get; set; }
    
    public FilePath VersionFile => _context.MakeAbsolute(_context.File(VersionFilePath));
    public FilePath VersionPropsFile => _context.MakeAbsolute(_context.File(VersionPropsFilePath));
  }
}