using System;
using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class EmailAddressField : FieldTemplateUserControl {
    public override Control DataControl => HyperLink1;

    protected override void OnDataBinding(EventArgs e) {
      var url = FieldValueString;
      if (!url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) url = "mailto:" + url;
      HyperLink1.NavigateUrl = url;
    }
  }
}