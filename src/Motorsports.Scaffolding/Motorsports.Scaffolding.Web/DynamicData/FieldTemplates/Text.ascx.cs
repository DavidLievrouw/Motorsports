using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class TextField : FieldTemplateUserControl {
    const int MAX_DISPLAYLENGTH_IN_LIST = 25;

    public override string FieldValueString {
      get {
        var value = base.FieldValueString;
        if (ContainerType == ContainerType.List) {
          if (value != null && value.Length > MAX_DISPLAYLENGTH_IN_LIST) value = value.Substring(0, length: MAX_DISPLAYLENGTH_IN_LIST - 3) + "...";
        }
        return value;
      }
    }

    public override Control DataControl => Literal1;
  }
}