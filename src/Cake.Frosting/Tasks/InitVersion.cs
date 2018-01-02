using System;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.FileHelpers;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(InitVersion))]
  public sealed class InitVersion : FrostingTaskWithProps<InitVersionProps> {
    /// <summary>
    /// - Updates version.props
    /// - Adds key to _Props.Items: AssemblyVersion
    /// </summary>
    public override void Run(ICakeContext context) {
      var props = GetProperties(context);

      var productVersion = context.FileReadText(props.VersionFile);
      var assemblyVersion = $"{productVersion}.0";

      props.AssemblyVersion = new Version(assemblyVersion);
      
      context.Information("Product version   = " + productVersion);
      context.Information("Assembly version  = " + assemblyVersion);

      var content = string.Format(@"
        <?xml version=""1.0"" encoding=""utf-8""?>
        <Project>
          <PropertyGroup>
            <Version>{0}</Version>
            <InformationalVersion>{1}</InformationalVersion>
          </PropertyGroup>
        </Project>".Replace("			", "").Trim(),
        assemblyVersion,
        assemblyVersion);

      context.FileWriteText(props.VersionPropsFile, content);
    }
  }
}