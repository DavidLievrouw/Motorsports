using System;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.IIS;

namespace Build.Tasks {
  public sealed class IISApplicationProps : GlobalProps {
    readonly ICakeContext _context;

    public IISApplicationProps(ICakeContext context) : base(context) {
      _context = context ?? throw new ArgumentNullException(nameof(context));

      PublishTargetDirectoryPath = RepoRootDirectoryPath + "/pub";

      IISApplicationPoolSettings = new ApplicationPoolSettings {
        Name = ProductName,
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