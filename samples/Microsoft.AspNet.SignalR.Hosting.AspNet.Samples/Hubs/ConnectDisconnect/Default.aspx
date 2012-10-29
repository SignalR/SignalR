<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Connection Status</li>
    </ul>

    <div class="page-header">
        <h2>Connection Status <small>Handling client connection state</small></h2>
        <p>Demonstrates how to handle the events that are raised when connections connect, reconnect and disconnect from the Hub API.</p>
    </div>

    <h4>Connection Status Messages</h4>
    <ul id="messages">
    </ul>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="Status.js"></script>
</asp:Content>
