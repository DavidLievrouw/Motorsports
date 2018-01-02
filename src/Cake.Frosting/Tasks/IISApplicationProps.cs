using System;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.IIS;

namespace Build.Tasks {
  public sealed class IISApplicationProps : TaskProps {
    readonly FrostingContext _context;

    public IISApplicationProps(FrostingContext context) : base(new GlobalProps(context)) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      PublishTargetDirectoryPath = GlobalProps.RepoRootDirectoryPath + "/pub";

      IISApplicationPoolSettings = new ApplicationPoolSettings {
        Name = GlobalProps.ProductName,
        Autostart = true
      };
      IISApplicationSettings = new ApplicationSettings() {
        SiteName = "Default Web Site",
        ApplicationPool = IISApplicationPoolSettings.Name,
        ApplicationPath = "/Motorsports",
        VirtualDirectory = "/",
        PhysicalDirectory = PublishTargetDirectory
      };
    }

    public string PublishTargetDirectoryPath { get; set; }

    public DirectoryPath PublishTargetDirectory => _context.MakeAbsolute(_context.Directory(PublishTargetDirectoryPath));
    public ApplicationPoolSettings IISApplicationPoolSettings { get; set; }
    public ApplicationSettings IISApplicationSettings { get; set; }
  }
}