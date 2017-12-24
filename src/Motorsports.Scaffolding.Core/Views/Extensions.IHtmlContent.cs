using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Motorsports.Scaffolding.Core.Views {
  public static partial class Extensions {
    public static string ToHtmlString(this IHtmlContent htmlContent) {
      var builder = new StringBuilder();
      using (var output = new StringWriter(builder)) {
        htmlContent.WriteTo(output, HtmlEncoder.Default);
      }
      return builder.ToString().Trim();
    }
  }
}