<%@ Control Language="C#" CodeBehind="ForeignKey.ascx.cs" Inherits="Motorsports.Scaffolding.ForeignKeyField" %>

<asp:HyperLink ID="HyperLink1" runat="server"
    Text="<%# GetDisplayString() %>"
    NavigateUrl="<%# GetNavigateUrl() %>"  />

