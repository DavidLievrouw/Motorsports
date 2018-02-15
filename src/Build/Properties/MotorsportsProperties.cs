using Cake.Core;

namespace Motorsports.Build.Properties {
  public class MotorsportsProperties : Properties<MotorsportsProperties> {
    public MotorsportsProperties(ICakeContext context) : base(context) {
      Arguments = new ArgumentsProperties(context, this);
      FileSystem = new FileSystemProperties(context, this);
      IIS = new IISProperties(context, this);
    }

    public string ProductName { get; } = "Motorsports";
    public string ProductCodeName { get; } = "Motorsports";
    public string ProductVersion { get; set; } = "1.0.0";
    public string AssemblyVersion { get; set; } = "1.0.0.0";

    public ArgumentsProperties Arguments { get; }
    public FileSystemProperties FileSystem { get; }
    public IISProperties IIS { get; }
  }
}