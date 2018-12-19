using System;
using Cake.Core;
using Cake.IIS;

namespace Motorsports.Build.Properties {
  public class IISApplicationPoolProperties : Properties<IISApplicationPoolProperties> {
    readonly MotorsportsProperties _container;

    public IISApplicationPoolProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public string Name => _container.ProductCodeName;
    public bool Autostart => true;

    public ApplicationPoolSettings ToApplicationPoolSettings() {
      return new ApplicationPoolSettings {
        Name = Name,
        Autostart = Autostart
      };
    }
  }
}