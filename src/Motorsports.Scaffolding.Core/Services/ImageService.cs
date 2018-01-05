using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IImageService {
    string GetSportLogo(string sport);
  }

  public class ImageService : IImageService {
    readonly IPhysicalPathResolver _physicalPathResolver;
    readonly IUrlHelper _urlHelper;

    public ImageService(IPhysicalPathResolver physicalPathResolver, IUrlHelper urlHelper) {
      _physicalPathResolver = physicalPathResolver ?? throw new ArgumentNullException(nameof(physicalPathResolver));
      _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
    }

    public string GetSportLogo(string sport) {
      var relativePath = "~/img/" + sport + ".png";
      var physicalPath = _physicalPathResolver.Resolve(relativePath);
      return File.Exists(physicalPath)
        ? _urlHelper.Content(relativePath)
        : _urlHelper.Content("~/img/notfound.png");
    }
  }
}