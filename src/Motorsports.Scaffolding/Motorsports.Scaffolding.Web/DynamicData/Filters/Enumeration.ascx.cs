using System;
using System.Linq;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.Filters {
  public partial class EnumerationFilter : QueryableFilterUserControl {
    const string NullValueString = "[null]";

    public override Control FilterControl => DropDownList1;

    public void Page_Init(object sender, EventArgs e) {
      if (!Page.IsPostBack) {
        if (!Column.IsRequired) DropDownList1.Items.Add(item: new ListItem("[Not Set]", NullValueString));
        PopulateListControl(DropDownList1);
        // Set the initial value if there is one
        var initialValue = DefaultValue;
        if (!string.IsNullOrEmpty(initialValue)) DropDownList1.SelectedValue = initialValue;
      }
    }

    public override IQueryable GetQueryable(IQueryable source) {
      var selectedValue = DropDownList1.SelectedValue;
      if (string.IsNullOrEmpty(selectedValue)) return source;

      object value = selectedValue;
      if (selectedValue == NullValueString) value = null;
      if (DefaultValues != null) DefaultValues[Column.Name] = value;
      return ApplyEqualityFilter(source, Column.Name, value);
    }

    protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e) {
      OnFilterChanged();
    }
  }
}