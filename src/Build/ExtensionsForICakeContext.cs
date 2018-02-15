using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

namespace Motorsports.Build {
  public static class ExtensionsForICakeContext {
    public static DirectoryPath GetAbsoluteDirectoryPath(this ICakeContext context, string path) {
      return context.MakeAbsolute(context.Directory(path));
    }

    public static FilePath GetAbsoluteFilePath(this ICakeContext context, string path) {
      return context.MakeAbsolute(context.File(path));
    }
  }
}