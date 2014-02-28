<%@ Page Title="ASP.NET SignalR: Simultaneous Connections Demo" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.SimultaneousConnections.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Simultaneous Connections</li>
    </ul>

    <div class="page-header">
        <h2>Simultaneous Connections </h2>
        <p>Demonstrates simultaneous connections. You can add new connections via the button or default the number of active connections via the queryString parameters hubConnections and persistentConnections.</p>
    </div>

    <p>
        <input type="button" id="addHubCon" class="btn" value="Add new Hub Connection" />
        <input type="button" id="addPCon" class="btn" value="Add new Persistent Connection" />
    </p>

    <div id="connections">
    </div>

    <h5>Messages: latest on top</h5>
    <ul id="messages">
    </ul>


</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="SimultaneousConnections.js"></script>
</asp:Content>
