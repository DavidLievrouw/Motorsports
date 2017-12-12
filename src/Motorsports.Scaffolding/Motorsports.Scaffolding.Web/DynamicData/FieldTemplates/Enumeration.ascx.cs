using System;
using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class EnumerationField : FieldTemplateUserControl {
    public override Control DataControl => Literal1;

    public string EnumFieldValueString {
      get {
        if (FieldValue == null) return FieldValueString;

        var enumType = Column.GetEnumType();
        if (enumType != null) {
          var enumValue = Enum.ToObject(enumType, FieldValue);
          return FormatFieldValue(enumValue);
        }

        return FieldValueString;
      }
    }
  }
}