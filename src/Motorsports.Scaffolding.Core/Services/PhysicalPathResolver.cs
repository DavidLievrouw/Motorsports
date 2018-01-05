using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IPhysicalPathResolver {
    string Resolve(string relativePath);
  }

  public class PhysicalPathResolver : IPhysicalPathResolver {
    readonly IHostingEnvironment _hostingEnvironment;
    readonly IUrlHelper _urlHelper;

    public PhysicalPathResolver(IUrlHelper urlHelper, IHostingEnvironment hostingEnvironment) {
      _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
      _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
    }

    public string Resolve(string relativePath) {
      if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
      if (Path.IsPathRooted(relativePath)) return relativePath;

      var resolvedRelativePath = _urlHelper.Content(relativePath);
      var webroot = _hostingEnvironment.WebRootPath;
      return Path.Combine(webroot, resolvedRelativePath.Substring(1).Replace("/", "\\"));
    }
  }
}