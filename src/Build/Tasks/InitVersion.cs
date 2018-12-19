using System;
using Cake.Common.Diagnostics;
using Cake.FileHelpers;
using Cake.Frosting;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(InitVersion))]
  public sealed class InitVersion : FrostingTask<Context> {
    public override void Run(Context context) {
      var productVersion = context.FileReadText(context.Motorsports.FileSystem.VersionFile);
      var revisionNumber = 0;
      var assemblyVersion = $"{productVersion}.{revisionNumber}";

      /* Derivates from determined versions */
      var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmZ");
      var assemblyInformationalVersion = $"{assemblyVersion} {timestamp}";

      context.Information("Product version       = " + productVersion);
      context.Information("Assembly version      = " + assemblyVersion);
      context.Information("InformationalVersion  = " + assemblyInformationalVersion);

      /* Update version.props */
      var content = string.Format(
        @"
      <?xml version=""1.0"" encoding=""utf-8""?>
      <Project>
          <PropertyGroup>
              <Version>{0}</Version>
              <InformationalVersion>{1}</InformationalVersion>
          </PropertyGroup>
      </Project>
      ".Replace("			", "")
          .Trim(),
        assemblyVersion,
        assemblyInformationalVersion);
      context.FileWriteText(context.Motorsports.FileSystem.VersionPropsFile, content);

      /* Set versions in Context */
      context.Motorsports.ProductVersion = productVersion;
      context.Motorsports.AssemblyVersion = assemblyVersion;
    }
  }
}