using System;
using System.Collections.Specialized;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.FieldTemplates {
  public partial class Enumeration_EditField : FieldTemplateUserControl {
    Type _enumType;

    Type EnumType {
      get {
        if (_enumType == null) _enumType = Column.GetEnumType();
        return _enumType;
      }
    }

    public override Control DataControl => DropDownList1;

    protected void Page_Load(object sender, EventArgs e) {
      DropDownList1.ToolTip = Column.Description;

      if (DropDownList1.Items.Count == 0) {
        if (Mode == DataBoundControlMode.Insert || !Column.IsRequired) DropDownList1.Items.Add(item: new ListItem("[Not Set]", string.Empty));
        PopulateListControl(DropDownList1);
      }

      SetUpValidator(RequiredFieldValidator1);
      SetUpValidator(DynamicValidator1);
    }

    protected override void OnDataBinding(EventArgs e) {
      base.OnDataBinding(e);

      if (Mode == DataBoundControlMode.Edit && FieldValue != null) {
        var selectedValueString = GetSelectedValueString();
        var item = DropDownList1.Items.FindByValue(selectedValueString);
        if (item != null) DropDownList1.SelectedValue = selectedValueString;
      }
    }

    protected override void ExtractValues(IOrderedDictionary dictionary) {
      var value = DropDownList1.SelectedValue;
      if (value == string.Empty) value = null;
      dictionary[Column.Name] = ConvertEditedValue(value);
    }
  }
}