using System;
using Cake.Core;
using Cake.Core.IO;

namespace Motorsports.Build.Properties {
  public class ProjectsAndSolutionsProperties : Properties<ProjectsAndSolutionsProperties> {
    readonly MotorsportsProperties _container;

    public ProjectsAndSolutionsProperties(ICakeContext context, MotorsportsProperties container) : base(context) {
      _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public DirectoryPath ScaffoldingProjectDirectory => Context.GetAbsoluteDirectoryPath(_container.FileSystem.SourceDirectory + "/Motorsports.Scaffolding.Core");
    public FilePath ScaffoldingProjectFile => Context.GetAbsoluteFilePath(ScaffoldingProjectDirectory + "/Motorsports.Scaffolding.Core.csproj");
    public DirectoryPath ScaffoldingTargetDirectory => _container.FileSystem.PublishTargetDirectory;
  }
}