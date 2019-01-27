using System;
using Cake.Core;

namespace Motorsports.Build.Properties {
  public class IISProperties : Properties<IISProperties> {
    readonly MotorsportsProperties _container;

    public IISProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
      IISApplicationPool = new IISApplicationPoolProperties(context, container);
      ScaffoldingIISApplication = new IISApplicationProperties(context, container) {
        ApplicationPath = "/" + _container.ProductName,
        PhysicalDirectory = _container.FileSystem.ProjectsAndSolutions.ScaffoldingTargetDirectory
      };
    }

    public IISApplicationPoolProperties IISApplicationPool { get; }
    public IISApplicationProperties ScaffoldingIISApplication { get; set; }
  }
}