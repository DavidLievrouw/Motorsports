#addin nuget:?package=Cake.Compression&version=0.1.4
#addin nuget:?package=Cake.IIS&version=0.3.1
#addin nuget:?package=Cake.FileHelpers&version=2.0.0
#addin nuget:?package=SharpZipLib&version=0.86.0

// Arguments
public string Target { get; set; } = Argument<string>("target", "");
public string Configuration { get; } = Argument<string>("configuration", "Release");
public string Verbosity { get; } = Argument<string>("verbosity", "Verbose");

// Constants
public string ProductName { get; set; } = "Motorsports";
public string RepoRootDirectoryPath { get; set; } = ".";
public string SourceDirectoryPath { get; set; } = RepoRootDirectoryPath + "/src";
public string PublishDirectoryPath { get; set; } = "./pub";
public string ReleaseDirectoryPath { get; set; } = "./dist";
public string VersionFilePath { get; set; } = RepoRootDirectoryPath + "/version.txt";
public string ScaffoldingProjectFilePath { get; set; } = SourceDirectoryPath + "/Motorsports.Scaffolding.Core/Motorsports.Scaffolding.Core.csproj";

// Resolved properties
public DirectoryPath RepoRootDirectory { get; set; }
public DirectoryPath SourceDirectory { get; set; }
public DirectoryPath PublishDirectory { get; set; }
public DirectoryPath ReleaseDirectory { get; set; }
public FilePath ScaffoldingProjectFile { get; set; }
public FilePath VersionFile { get; set; }
public Version AssemblyVersion { get; set; }
public DotNetCoreVerbosity DotNetCoreVerbosity { get; set; }
public ApplicationPoolSettings IISApplicationPoolSettings { get; set; }
public ApplicationSettings IISApplicationSettings { get; set; }

Task("InitProps")
  .Does(() => {
    switch (Verbosity.ToLower()) {
      case "quiet":
        DotNetCoreVerbosity = DotNetCoreVerbosity.Quiet;
        break;
      case "minimal":
        DotNetCoreVerbosity = DotNetCoreVerbosity.Minimal;
        break;
      case "normal":
        DotNetCoreVerbosity = DotNetCoreVerbosity.Normal;
        break;
      case "verbose":
        DotNetCoreVerbosity = DotNetCoreVerbosity.Detailed;
        break;
      case "diagnostic":
        DotNetCoreVerbosity = DotNetCoreVerbosity.Diagnostic;
        break;
      default:
        DotNetCoreVerbosity = DotNetCoreVerbosity.Normal;
        break;
    }
    Information("DotNetCore verbosity = " + DotNetCoreVerbosity);
    
    RepoRootDirectory = MakeAbsolute(Directory(RepoRootDirectoryPath));
    Information("Repostory root path = " + RepoRootDirectory);
    
    SourceDirectory = MakeAbsolute(Directory(SourceDirectoryPath));
    Information("Source directory = " + SourceDirectory);
    
    PublishDirectory = MakeAbsolute(Directory(PublishDirectoryPath));
    Information("Publish directory = " + PublishDirectory);
    
    ReleaseDirectory = MakeAbsolute(Directory(ReleaseDirectoryPath));
    Information("Release directory = " + ReleaseDirectory);
    
    ScaffoldingProjectFile = MakeAbsolute(File(ScaffoldingProjectFilePath));
    Information("Scaffolding project file = " + ScaffoldingProjectFile);
    
    VersionFile = MakeAbsolute(File(VersionFilePath));
    Information("Version file = " + VersionFile);
        
    var productVersion = FileReadText(VersionFile);
    AssemblyVersion = new Version(string.Format("{0}.{1}", productVersion, "0"));
    Information("Product version = " + AssemblyVersion.ToString(3));
    Information("Assembly version = " + AssemblyVersion.ToString(4));
    
    IISApplicationPoolSettings = new ApplicationPoolSettings {
      Name = ProductName,
      Autostart = true
    };
    IISApplicationSettings = new ApplicationSettings() {
      SiteName = "Default Web Site",
      ApplicationPool = IISApplicationPoolSettings.Name,
      ApplicationPath = "/Motorsports",
      VirtualDirectory = "/",
      PhysicalDirectory = PublishDirectory
    };
  });

Task("InitVersion")
  .IsDependentOn("InitProps")
  .Does(() => {
    var content = string.Format(@"
      <?xml version=""1.0"" encoding=""utf-8""?>
      <Project>
        <PropertyGroup>
          <Version>{0}</Version>
          <InformationalVersion>{1}</InformationalVersion>
        </PropertyGroup>
      </Project>".Replace("			", "").Trim(),
      AssemblyVersion.ToString(4),
      AssemblyVersion.ToString(4));

    FileWriteText("./version.props", content);
  });
  
Task("StopIISApplicationPoolIfExists")
  .WithCriteria(() => PoolExists(IISApplicationPoolSettings.Name))
  .Does(() => { 
    StopPool(IISApplicationPoolSettings.Name);
    System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5)).Wait(); // Make sure it is stopped
  });
  
Task("Publish")
  .IsDependentOn("InitVersion")
  .IsDependentOn("StopIISApplicationPoolIfExists")
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

Task("RemoveIISApplicationIfExists")
  .WithCriteria(() => SiteApplicationExists(IISApplicationSettings))
  .Does(() => RemoveSiteApplication(IISApplicationSettings));
  
Task("RemoveIISApplicationPoolIfExists")
  .IsDependentOn("StopIISApplicationPoolIfExists")
  .WithCriteria(() => PoolExists(IISApplicationPoolSettings.Name))
  .Does(() => { 
    DeletePool(IISApplicationPoolSettings.Name); 
  });
  
Task("RemoveIISApplication")
  .IsDependentOn("InitProps")
  .IsDependentOn("RemoveIISApplicationIfExists")
  .IsDependentOn("RemoveIISApplicationPoolIfExists");

Task("CreateIISApplication")
  .IsDependentOn("InitProps")
  .IsDependentOn("RemoveIISApplication")
  .Does(() => {
    CreatePool(IISApplicationPoolSettings);
    AddSiteApplication(IISApplicationSettings);   
  });
   
Task("CreateRelease")
  .IsDependentOn("Publish")
  .Does(() => {
    CleanDirectory(ReleaseDirectory);
    ZipCompress(PublishDirectory, ReleaseDirectory + "/Motorsports.v" + AssemblyVersion.ToString(3) + ".zip");
  });
  
RunTarget(Target);