using System;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.FileHelpers;
using Cake.Frosting;

namespace Motorsports.Build.Tasks {
  [TaskName(nameof(InitVersion))]
  public sealed class InitVersion : FrostingTask<Context> {
    public override void Run(Context context) {
      string assemblyVersion;
      var productVersion = context.FileReadText(context.Motorsports.FileSystem.VersionFile);
      if (context.BuildSystem().TeamCity.IsRunningOnTeamCity) {
        // Check if TeamCity has specified a full version number (major.minor.build.revision) or just a revision number
        var tcBuildNumber = context.BuildSystem().TeamCity.Environment.Build.Number;
        var isCompleteVersionNumber = Version.TryParse(tcBuildNumber.Trim(), out var parsedVersion);
        if (isCompleteVersionNumber) {
          // TeamCity has specified a full version number (major.minor.build.revision), so ignore the version.txt file
          assemblyVersion = parsedVersion.ToString(4);
          productVersion = parsedVersion.ToString(3);
        }
        else {
          // TeamCity has only specified a revision number
          assemblyVersion = $"{productVersion}.{tcBuildNumber}";
        }
      }
      else {
        // Not running in TC, use version.txt
        var revisionNumber = 0;
        assemblyVersion = $"{productVersion}.{revisionNumber}";
      }

      /* Derivates from determined versions */
      var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmZ");
      var assemblyInformationalVersion = $"{assemblyVersion} {timestamp}";

      context.Information("Product version       = " + productVersion);
      context.Information("Assembly version      = " + assemblyVersion);
      context.Information("InformationalVersion  = " + assemblyInformationalVersion);

      /* Update version.props */
      var content = string.Format(@"
      <?xml version=""1.0"" encoding=""utf-8""?>
      <Project>
          <PropertyGroup>
              <Version>{0}</Version>
              <InformationalVersion>{1}</InformationalVersion>
          </PropertyGroup>
      </Project>
      ".Replace("			", "").Trim(), assemblyVersion, assemblyInformationalVersion);
      context.FileWriteText(context.Motorsports.FileSystem.VersionPropsFile, content);

      /* Let TC know the resolved build number */
      if (context.BuildSystem().TeamCity.IsRunningOnTeamCity) {
        context.Information("Setting TeamCity build number to " + assemblyVersion);
        context.BuildSystem().TeamCity.SetBuildNumber(assemblyVersion);
      }

      /* Set versions in Context */
      context.Motorsports.ProductVersion = productVersion;
      context.Motorsports.AssemblyVersion = assemblyVersion;
    }
  }
}