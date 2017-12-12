using System;
using System.Linq;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.Filters {
  public partial class BooleanFilter : QueryableFilterUserControl {
    const string NullValueString = "[null]";

    public override Control FilterControl => DropDownList1;

    protected void Page_Init(object sender, EventArgs e) {
      if (!Column.ColumnType.Equals(o: typeof(bool)))
        throw new InvalidOperationException(message: string.Format("A boolean filter was loaded for column '{0}' but the column has an incompatible type '{1}'.", Column.Name, Column.ColumnType));

      if (!Page.IsPostBack) {
        DropDownList1.Items.Add(item: new ListItem("All", string.Empty));
        if (!Column.IsRequired) DropDownList1.Items.Add(item: new ListItem("[Not Set]", NullValueString));
        DropDownList1.Items.Add(item: new ListItem("True", bool.TrueString));
        DropDownList1.Items.Add(item: new ListItem("False", bool.FalseString));
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