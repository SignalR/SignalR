<%@ Page Title="" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.Counting.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Counting</li>
    </ul>

    <div class="page-header">
        <h2>Counting</h2>
        <p>Client acks</p>
    </div>

    <input type="button" value="Go" id="go" />
    <div id="value"></div>
    
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script>
        var options = { transport: activeTransport };
    </script>
    <script src="ClientAcks.js"></script>
</asp:Content>

