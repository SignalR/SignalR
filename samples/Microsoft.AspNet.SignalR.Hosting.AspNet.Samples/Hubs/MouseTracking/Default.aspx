<%@ Page Title="ASP.NET SignalR: Mouse Tracking" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.MouseTracking.Default" %>

<asp:Content runat="server" ContentPlaceHolderID="HeadContent">
    <link href="MouseTracking.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Mouse Tracking</li>
    </ul>

    <div class="page-header">
        <h2>Mouse Tracking</h2>
        <p>An example that displays a live cursor on all users' screens for all other users' mouse movements.</p>
    </div>

</asp:Content>

<asp:Content runat="server" ContentPlaceHolderID="Scripts">
    <script src="MouseTracking.js"></script>
</asp:Content>
