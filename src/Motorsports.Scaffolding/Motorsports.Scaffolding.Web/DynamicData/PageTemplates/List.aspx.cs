using System;
using System.Web.DynamicData;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Motorsports.Scaffolding.Web.DynamicData.PageTemplates {
  public partial class List : Page {
    protected MetaTable table;

    protected void Page_Init(object sender, EventArgs e) {
      table = DynamicDataRouteHandler.GetRequestMetaTable(Context);
      GridView1.SetMetaTable(table, defaultValues: table.GetColumnValuesFromRoute(Context));
      GridDataSource.EntityTypeFilter = table.EntityType.Name;
    }

    protected void Page_Load(object sender, EventArgs e) {
      Title = table.DisplayName;
      GridDataSource.Include = table.ForeignKeyColumnsNames;

      // Disable various options if the table is readonly
      if (table.IsReadOnly) {
        GridView1.Columns[0].Visible = false;
        InsertHyperLink.Visible = false;
        GridView1.EnablePersistedSelection = false;
      }
    }

    protected void Label_PreRender(object sender, EventArgs e) {
      var label = (Label) sender;
      var dynamicFilter = (DynamicFilter) label.FindControl("DynamicFilter");
      var fuc = dynamicFilter.FilterTemplate as QueryableFilterUserControl;
      if (fuc != null && fuc.FilterControl != null) label.AssociatedControlID = fuc.FilterControl.GetUniqueIDRelativeTo(label);
    }

    protected override void OnPreRenderComplete(EventArgs e) {
      var routeValues = new RouteValueDictionary(dictionary: GridView1.GetDefaultValues());
      InsertHyperLink.NavigateUrl = table.GetActionPath(PageAction.Insert, routeValues);
      base.OnPreRenderComplete(e);
    }

    protected void DynamicFilter_FilterChanged(object sender, EventArgs e) {
      GridView1.PageIndex = 0;
    }
  }
}