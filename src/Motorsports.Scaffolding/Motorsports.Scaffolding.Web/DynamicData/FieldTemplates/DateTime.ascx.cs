using System.Web.DynamicData;
using System.Web.UI;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class DateTimeField : FieldTemplateUserControl {
    public override Control DataControl => Literal1;
  }
}