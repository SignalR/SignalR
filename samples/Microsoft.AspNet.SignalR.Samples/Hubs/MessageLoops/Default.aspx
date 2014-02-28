<%@ Page Title="ASP.NET SignalR: Message Loops Demo" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.MesssagesLoops.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Message Loops</li>
    </ul>

    <div class="page-header">
        <h2>Message Loops </h2>
        <p>Demonstrates message loops where client calls the server method to broadcast message after the client successfully calls the server method once start, and shows missing and dup messages if happens.</p>
    </div>

    <p>
        <input type="radio" id="radioAll" name="group1" value="all" checked="checked">
        Send message to all<br>
        <input type="radio" id="radioGroup" name="group1" value="group">
        Send message to group<br>
        <input type="radio" id="radioCaller" name="group1" value="value">
        Send message to caller<br>
    </p>

    <p>
        <input type="button" id="startMessageLoops" class="btn" value="Start Message Loops" />
        send each message in
        <input type="text" id="sleep" value="5000" style="width: 40px;" />milliseconds
    </p>

    <p>
        <button id="stopStart" class="btn" disabled="disabled"><span>Stop Connection</span></button>
    </p>

    <h5>Messages: </h5>
    <div id="messageLoops">
    </div>
    <label id="missingMessagesCount">
    </label>

    <ul id="messages">
    </ul>


</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="MessageLoops.js"></script>
</asp:Content>
