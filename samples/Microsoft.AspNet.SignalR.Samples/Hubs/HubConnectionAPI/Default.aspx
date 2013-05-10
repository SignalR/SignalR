<%@ Page Title="ASP.NET SignalR: Hub Connection API Demo" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.HubConnectionAPI.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Hub Connection API</li>
    </ul>

    <div class="page-header">
        <h2>Hub Connection API</h2>
        <p>Demonstrates Hub Connection API including starting and stopping, sending and receiving messages, and managing groups.</p>
    </div>

    <p>
        <label for="group">Group: </label>
        <input type="text" id="group" placeholder="Group Name" />
        <input type="button" id="joinGroup" class="btn" value="Join Group" />
        <input type="button" id="leaveGroup" class="btn" value="Leave Group" />
    </p>

    <p>
        <label for="connection">Specified ConnectionID: </label>
        <input type="text" id="connection" style="width: 460px" placeholder="connectionId" />
    </p>

    <div>
        <h4>To Everybody</h4>
        <div>
            <input type="text" id="message" placeholder="Message" />
            <input type="button" id="broadcast" class="btn" value="Broadcast" />
            <input type="button" id="broadcastExceptSpecified" class="btn" value="Broadcast (All Except specified Connection)" />
            <input type="button" id="other" class="btn" value="Other (Everyone but me)" />
        </div>
    </div>

    <div>
        <h4>To Group</h4>
        <div>
            <input type="text" id="groupMessage" placeholder="Message" />
            <input type="button" id="groupmsg" class="btn" value="Send to group" />
            <input type="button" id="groupmsgExceptSpecified" class="btn" value="Send to group (Except specified Connection)" />
            <input type="button" id="otherInGroupmsg" class="btn" value="Send to Other In Group" />
        </div>
    </div>

    <div>
        <h4>To Me /specified</h4>
        <div>
            <input type="text" id="me" placeholder="Message" />
            <input type="button" id="sendToMe" class="btn" value="Send to me" />
            <input type="button" id="specified" class="btn" value="Send to specified connection" />
        </div>
    </div>

    <button id="stopStart" class="btn btn-info btn-small" disabled="disabled"><i class="icon-stop icon-white"></i><span>Stop Connection</span></button>

    <h5>Messages</h5>
    <ul id="messages">
    </ul>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="HubConnectionAPI.js"></script>
</asp:Content>
