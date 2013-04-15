<%@ Page Title="ASP.NET SignalR: Messages Loops Demo" Language="C#"  MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.MesssagesLoops.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Messages Loops</li>
    </ul>

    <div class="page-header">
        <h2>Messages Loops </h2>
        <p>Demonstrates messages loops where client call the server method to broadcast message after the client successfully call the server method, and show missing and dup messages if happens.
        </p>
    </div>
            
    <input type="button" id="sendMessageCount" value="Start messages loops" />     
     <br />
    
    <button id="stopStart" class="btn btn-info btn-small" disabled="disabled"><i class="icon-stop icon-white"></i> <span>Stop Connection</span></button>
    
    <div id="message"  >        
    </div> 
    <label id="missedMessagesCount">            
    </label>
    <br />
    <ul id="messages">
    </ul>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="MessagesLoops.js"></script>
</asp:Content>
