using System;
using Cake.Core;
using Cake.Core.IO;
using Cake.IIS;

namespace Motorsports.Build.Properties {
  public class IISApplicationProperties : Properties<IISApplicationProperties> {
    readonly MotorsportsProperties _container;

    public IISApplicationProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
      PhysicalDirectory = container.FileSystem.PublishTargetDirectory.FullPath;
      ApplicationPath = "/";
    }

    public DirectoryPath PhysicalDirectory { get; set; }
    public string ApplicationPath { get; set; }
    public string SiteName => "Default Web Site";
    public string VirtualDirectory => "/";
    public string ApplicationPoolName => _container.IIS.IISApplicationPool.Name;

    public ApplicationSettings ToApplicationSettings() {
      return new ApplicationSettings {
        SiteName = SiteName,
        ApplicationPool = ApplicationPoolName,
        ApplicationPath = ApplicationPath,
        VirtualDirectory = VirtualDirectory,
        PhysicalDirectory = PhysicalDirectory
      };
    }
  }
}