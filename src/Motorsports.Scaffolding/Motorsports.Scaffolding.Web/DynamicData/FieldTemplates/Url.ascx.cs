using System;
using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class UrlField : FieldTemplateUserControl {
    public override Control DataControl => HyperLinkUrl;

    protected override void OnDataBinding(EventArgs e) {
      HyperLinkUrl.NavigateUrl = ProcessUrl(FieldValueString);
    }

    string ProcessUrl(string url) {
      if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;

      return "http://" + url;
    }
  }
}