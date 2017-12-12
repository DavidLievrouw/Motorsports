<%@ Control Language="C#" CodeBehind="ForeignKey.ascx.cs" Inherits="Motorsports.Scaffolding.Web.DynamicData.FieldTemplates.ForeignKeyField" %>

<asp:HyperLink ID="HyperLink1" runat="server"
               Text="<%# GetDisplayString() %>"
               NavigateUrl="<%# GetNavigateUrl() %>"/>