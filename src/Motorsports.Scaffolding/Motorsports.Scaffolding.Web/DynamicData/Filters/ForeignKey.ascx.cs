using System;
using System.Collections;
using System.Linq;
using System.Web.DynamicData;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.Filters {
  public partial class ForeignKeyFilter : QueryableFilterUserControl {
    const string NullValueString = "[null]";

    new MetaForeignKeyColumn Column => (MetaForeignKeyColumn) base.Column;

    public override Control FilterControl => DropDownList1;

    protected void Page_Init(object sender, EventArgs e) {
      if (!Page.IsPostBack) {
        if (!Column.IsRequired) DropDownList1.Items.Add(item: new ListItem("[Not Set]", NullValueString));
        PopulateListControl(DropDownList1);
        // Set the initial value if there is one
        var initialValue = DefaultValue;
        if (!string.IsNullOrEmpty(initialValue)) DropDownList1.SelectedValue = initialValue;
      }
    }

    protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e) {
      OnFilterChanged();
    }

    public override IQueryable GetQueryable(IQueryable source) {
      var selectedValue = DropDownList1.SelectedValue;
      if (string.IsNullOrEmpty(selectedValue)) return source;

      if (selectedValue == NullValueString) return ApplyEqualityFilter(source, Column.Name, null);

      IDictionary dict = new Hashtable();
      Column.ExtractForeignKey(dict, selectedValue);
      foreach (DictionaryEntry entry in dict) {
        var key = (string) entry.Key;
        if (DefaultValues != null) DefaultValues[key] = entry.Value;
        source = ApplyEqualityFilter(source, propertyName: Column.GetFilterExpression(key), value: entry.Value);
      }
      return source;
    }
  }
}