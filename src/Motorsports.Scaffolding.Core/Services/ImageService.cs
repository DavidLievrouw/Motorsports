using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IImageService {
    string GetSportLogo(string sport);
    string GetSportLogo(string sport, out bool isFound);
  }

  public class ImageService : IImageService {
    readonly IFileProvider _fileProvider;
    readonly IUrlHelper _urlHelper;

    public ImageService(IFileProvider fileProvider, IUrlHelper urlHelper) {
      _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
      _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
    }

    public string GetSportLogo(string sport) {
      return GetSportLogo(sport, out var _);
    }

    public string GetSportLogo(string sport, out bool isFound) {
      var relativePath = _urlHelper.Content("~/img/" + sport.ToLowerInvariant() + ".png");
      var info = _fileProvider.GetFileInfo(relativePath);
      isFound = info.Exists;
      return isFound
        ? _urlHelper.Content(relativePath)
        : _urlHelper.Content("~/img/notfound.png");
    }
  }
}