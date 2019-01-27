using CommandLine;

namespace Motorsports.Build.Startup {
  public class Options {
    [Option('t', "target", Required = false, Default = "Default", HelpText = "The build script target to run.")]
    public string Target { get; set; }

    [Option('c', "configuration", Required = false, Default = ConfigurationOptions.Release, HelpText = "The build configuration to use")]
    public ConfigurationOptions Configuration { get; set; }

    [Option('v', "verbosity", Required = false, Default = VerbosityOptions.Normal, HelpText = "Specifies the amount of information to be displayed.")]
    public VerbosityOptions Verbosity { get; set; }

    [Option('p', "publishDirectory", Required = false, HelpText = "The directory to publish to")]
    public string PublishDirectory { get; set; }

    [Option('e', "environment", Required = false, Default = EnvironmentOptions.Production, HelpText = "The environment to use")]
    public EnvironmentOptions Environment { get; set; }

    [Option('i', "interactive", Required = false, Default = false, HelpText = "Ask the user interactively for options")]
    public bool Interactive { get; set; }

    public Options Clone() {
      return new Options {
        Interactive = Interactive,
        Configuration = Configuration,
        Environment = Environment,
        Verbosity = Verbosity,
        Target = Target,
        PublishDirectory = PublishDirectory
      };
    }
  }

  public enum ConfigurationOptions {
    Debug,
    Release
  }

  public enum VerbosityOptions {
    Quiet,
    Minimal,
    Normal,
    Verbose,
    Diagnostic
  }

  public enum EnvironmentOptions {
    Development,
    Staging,
    Production
  }
}