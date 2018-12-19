using System;
using Cake.Core;
using Cake.Core.IO;

namespace Motorsports.Build.Properties {
  public class FileSystemProperties : Properties<FileSystemProperties> {
    readonly MotorsportsProperties _container;

    public FileSystemProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
      ProjectsAndSolutions = new ProjectsAndSolutionsProperties(context, container);
    }

    public DirectoryPath RepoRootDirectory => Context.GetAbsoluteDirectoryPath(".");
    public DirectoryPath SourceDirectory => Context.GetAbsoluteDirectoryPath(RepoRootDirectory + "/src");

    public DirectoryPath PublishTargetDirectory => string.IsNullOrWhiteSpace(_container.Arguments.PublishDirectory)
      ? Context.GetAbsoluteDirectoryPath(RepoRootDirectory + "/dist")
      : Context.GetAbsoluteDirectoryPath(_container.Arguments.PublishDirectory);

    public FilePath VersionFile => Context.GetAbsoluteFilePath(RepoRootDirectory + "/version.txt");
    public FilePath VersionPropsFile => Context.GetAbsoluteFilePath(SourceDirectory + "/version.props");

    public ProjectsAndSolutionsProperties ProjectsAndSolutions { get; }
  }
}