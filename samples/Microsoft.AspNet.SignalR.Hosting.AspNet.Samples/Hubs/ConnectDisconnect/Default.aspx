<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Connection Status</li>
    </ul>

    <div class="page-header">
        <h2>Connection Status <small>Handling client connection state</small></h2>
        <p>Demonstrates how to handle the events that are raised when connections connect, reconnect and disconnect from the Hub API.</p>
    </div>

    <button id="connectionStart">START</button>
    <button id="connectionStop">STOP</button>    
    <button id="ping">PING</button>

    <h4>Connection Status Messages</h4>
    <ul id="messages">
    </ul>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/js") %>"></script>
    <script src="Status.js"></script>
</asp:Content>
