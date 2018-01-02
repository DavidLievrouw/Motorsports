using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks {
  public sealed class InitVersionProps : TaskProps {
    readonly FrostingContext _context;

    public InitVersionProps(FrostingContext context) : base(new GlobalProps(context)) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      VersionFilePath = GlobalProps.RepoRootDirectoryPath + "/version.txt";
      VersionPropsFilePath = GlobalProps.RepoRootDirectoryPath + "/version.props";
      
      _context.Information("Version file = " + VersionFile);
      _context.Information("Version props file = " + VersionPropsFilePath);
    }
    
    public string VersionFilePath { get; set; }
    public string VersionPropsFilePath { get; set; }
    
    public FilePath VersionFile => _context.MakeAbsolute(_context.File(VersionFilePath));
    public FilePath VersionPropsFile => _context.MakeAbsolute(_context.File(VersionPropsFilePath));
  }
}