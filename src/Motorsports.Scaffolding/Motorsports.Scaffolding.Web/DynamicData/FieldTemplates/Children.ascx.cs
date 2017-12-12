using System;
using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class ChildrenField : FieldTemplateUserControl {
    public string NavigateUrl { get; set; }

    public bool AllowNavigation { get; set; } = true;

    public override Control DataControl => HyperLink1;

    protected void Page_Load(object sender, EventArgs e) {
      HyperLink1.Text = "View " + ChildrenColumn.ChildTable.DisplayName;
    }

    protected string GetChildrenPath() {
      if (!AllowNavigation) return null;

      if (string.IsNullOrEmpty(NavigateUrl)) return ChildrenPath;
      else return BuildChildrenPath(NavigateUrl);
    }
  }
}