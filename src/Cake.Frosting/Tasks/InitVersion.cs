using Cake.Common.Diagnostics;
using Cake.FileHelpers;
using Cake.Frosting;

namespace Build.Tasks {
  [TaskName(nameof(InitVersion))]
  public sealed class InitVersion : FrostingTask<Context> {
    /// <summary>
    /// - Updates version.props
    /// - Adds key to _Props.Items: AssemblyVersion
    /// </summary>
    public override void Run(Context context) {
      var productVersion = context.FileReadText(Lifetime._Props.VersionFilePath);
      const int buildNumber = 0;
      var assemblyVersion = string.Format("{0}.{1}", productVersion, buildNumber.ToString());
      Lifetime._Props.Items.Add("AssemblyVersion", assemblyVersion);
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

      context.FileWriteText("./version.props", content);
    }
  }
}