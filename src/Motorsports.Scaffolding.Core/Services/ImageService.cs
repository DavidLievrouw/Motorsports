using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Motorsports.Scaffolding.Core.Services {
  public interface IImageService {
    string GetSportLogo(string sport);
  }

  public class ImageService : IImageService {
    readonly IFileProvider _fileProvider;
    readonly IUrlHelper _urlHelper;

    public ImageService(IFileProvider fileProvider, IUrlHelper urlHelper) {
      _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
      _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
    }

    public string GetSportLogo(string sport) {
      var relativePath = _urlHelper.Content("~/img/" + sport + ".png");
      return _urlHelper.Content(relativePath);
    }
  }
}