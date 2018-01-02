using Cake.Common.Diagnostics;
using Cake.FileHelpers;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(InitVersion))]
  public sealed class InitVersion : FrostingTask<FrostingContext> {
    /// <summary>
    /// - Updates version.props
    /// - Adds key to _Props.Items: AssemblyVersion
    /// </summary>
    public override void Run(FrostingContext context) {
      var productVersion = context.FileReadText(Lifetime.GlobalOptions.VersionFile);
      var assemblyVersion = $"{productVersion}.0";
      Lifetime.GlobalOptions.Items.Add("AssemblyVersion", assemblyVersion);
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

      context.FileWriteText(Lifetime.GlobalOptions.VersionPropsFile, content);
    }
  }
}