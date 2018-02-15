using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IImageService {
    string GetSportLogo(string sport);
    string GetSportLogo(string sport, out bool isFound);
  }

  public class ImageService : IImageService {
    readonly IPhysicalPathResolver _physicalPathResolver;
    readonly IUrlHelper _urlHelper;

    public ImageService(IPhysicalPathResolver physicalPathResolver, IUrlHelper urlHelper) {
      _physicalPathResolver = physicalPathResolver ?? throw new ArgumentNullException(nameof(physicalPathResolver));
      _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
    }

    public string GetSportLogo(string sport) {
      return GetSportLogo(sport, out var _);
    }

    public string GetSportLogo(string sport, out bool isFound) {
      var relativePath = "~/img/" + sport + ".png";
      var physicalPath = _physicalPathResolver.Resolve(relativePath);
      isFound = File.Exists(physicalPath);
      return isFound
        ? _urlHelper.Content(relativePath)
        : _urlHelper.Content("~/img/notfound.png");
    }
  }
}