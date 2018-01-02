#addin nuget:?package=Cake.Compression&version=0.1.4
#addin nuget:?package=Cake.IIS&version=0.3.1
#addin nuget:?package=Cake.FileHelpers&version=2.0.0
#addin nuget:?package=SharpZipLib&version=0.86.0

public string RepoRootDirectoryPath { get; set; } = ".";
public string SourceDirectoryPath { get; set; } = RepoRootDirectoryPath + "/src";
public string PublishDirectoryPath { get; set; } = "./dist";
public string VersionFilePath { get; set; } = RepoRootDirectoryPath + "/version.txt";
public string ScaffoldingProjectFilePath { get; set; } = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";

public DirectoryPath RepoRootDirectory => MakeAbsolute(Directory(RepoRootDirectoryPath));
public DirectoryPath SourceDirectory => MakeAbsolute(Directory(SourceDirectoryPath));
public DirectoryPath PublishDirectory => MakeAbsolute(Directory(PublishDirectoryPath));
public FilePath ScaffoldingProjectFile => MakeAbsolute(File(ScaffoldingProjectFilePath));
public FilePath VersionFile => MakeAbsolute(File(VersionFilePath));

public string Target { get; set; } = Argument<string>("target", "");
public string ProductName { get; set; } = "Motorsports";
public string Configuration { get; } = Argument<string>("configuration", "Release");
public string Verbosity { get; } = Argument<string>("verbosity", "Verbose");

public DotNetCoreVerbosity DotNetCoreVerbosity {
  get {
    switch (Verbosity.ToLower()) {
      case "quiet":
        return DotNetCoreVerbosity.Quiet;
      case "minimal":
        return DotNetCoreVerbosity.Minimal;
      case "normal":
        return DotNetCoreVerbosity.Normal;
      case "verbose":
        return DotNetCoreVerbosity.Detailed;
      case "diagnostic":
        return DotNetCoreVerbosity.Diagnostic;
      default:
        return DotNetCoreVerbosity.Normal;
    }
  }
}

Task("InitProps")
  .Does(() => {
  });

Task("InitVersion")
  .IsDependentOn("InitProps")
  .Does(() => {
    Information(RepoRootDirectoryPath);
    Information(RepoRootDirectory);
    Information(SourceDirectoryPath);
    Information(SourceDirectory);
    Information(VersionFilePath);
    Information(VersionFile);
    var productVersion = FileReadText(VersionFile);
    const int buildNumber = 0;
    var assemblyVersion = string.Format("{0}.{1}", productVersion, buildNumber.ToString());
    Information("Product version   = " + productVersion);
    Information("Assembly version  = " + assemblyVersion);
    
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

    FileWriteText("./version.props", content);
  });

Task("Publish")
  .IsDependentOn("InitVersion")
  .Does(() => {
    CleanDirectory(PublishDirectory);
    DotNetCoreRestore(ScaffoldingProjectFile.FullPath);
    DotNetCoreClean(
      ScaffoldingProjectFile.FullPath,
      new DotNetCoreCleanSettings {
        Verbosity = DotNetCoreVerbosity,
        Configuration = Configuration
      });
    DotNetCorePublish(
      ScaffoldingProjectFile.FullPath,
      new DotNetCorePublishSettings {
        Configuration = Configuration,
        OutputDirectory = PublishDirectory,
        Verbosity = DotNetCoreVerbosity,
        ArgumentCustomization = args => args.Append("--no-restore")
      });
  });

RunTarget(Target);