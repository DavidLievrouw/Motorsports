using System;
using System.Collections.Specialized;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class ForeignKey_EditField : FieldTemplateUserControl {
    public override Control DataControl => DropDownList1;

    protected void Page_Load(object sender, EventArgs e) {
      if (DropDownList1.Items.Count == 0) {
        if (Mode == DataBoundControlMode.Insert || !Column.IsRequired) DropDownList1.Items.Add(item: new ListItem("[Not Set]", ""));
        PopulateListControl(DropDownList1);
      }

      SetUpValidator(RequiredFieldValidator1);
      SetUpValidator(DynamicValidator1);
    }

    protected override void OnDataBinding(EventArgs e) {
      base.OnDataBinding(e);

      var selectedValueString = GetSelectedValueString();
      var item = DropDownList1.Items.FindByValue(selectedValueString);
      if (item != null) DropDownList1.SelectedValue = selectedValueString;
    }

    protected override void ExtractValues(IOrderedDictionary dictionary) {
      // If it's an empty string, change it to null
      var value = DropDownList1.SelectedValue;
      if (string.IsNullOrEmpty(value)) value = null;

      ExtractForeignKey(dictionary, value);
    }
  }
}