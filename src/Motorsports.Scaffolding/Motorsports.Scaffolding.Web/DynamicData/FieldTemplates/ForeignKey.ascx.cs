using System;
using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class ForeignKeyField : FieldTemplateUserControl {
    public string NavigateUrl { get; set; }

    public bool AllowNavigation { get; set; } = true;

    public override Control DataControl => HyperLink1;

    protected string GetDisplayString() {
      var value = FieldValue;

      if (value == null) return FormatFieldValue(fieldValue: ForeignKeyColumn.GetForeignKeyString(Row));
      else return FormatFieldValue(fieldValue: ForeignKeyColumn.ParentTable.GetDisplayString(value));
    }

    protected string GetNavigateUrl() {
      if (!AllowNavigation) return null;

      if (string.IsNullOrEmpty(NavigateUrl)) return ForeignKeyPath;
      else return BuildForeignKeyPath(NavigateUrl);
    }
  }
}