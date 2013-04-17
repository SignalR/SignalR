<%@ Page Title="ASP.NET SignalR: HubCleints APIs Demo" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.HubClientsAPIs.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">HubCleints APIs</li>
    </ul>

    <div class="page-header">
        <h2>Hub Clients APIs </h2>
        <p>Demonstrates Hub Clients APIs including starting and stopping, sending and receiving messages, and managing groups.</p>
    </div>

    <p>
        <label for="group">Group: </label>
        <input type="text" id="group" placeholder="Group name" />
        <input type="button" id="join-group" value="Join Group" />
        <input type="button" id="leave-group" value="Leave Group" />
    </p>

    <p>
        <label for="connection">Specified ConnectionID: </label>
        <input type="text" id="connection" placeholder="connectionId" />
    </p>

    <div>
        <h4>To Everybody</h4>
        <form>
            <input type="text" id="message" placeholder="Message" />
            <input type="button" id="broadcast" value="Broadcast" />
            <input type="button" id="broadcast-except-specified" value="Broadcast (All Except specified Connection)" />
            <input type="button" id="other" value="Other (Everyone but me)" />
        </form>
    </div>

    <div>
        <h4>To Group</h4>
        <form>
            <input type="text" id="groupMessage" placeholder="Message" />
            <input type="button" id="groupmsg" value="Send to group" />
            <input type="button" id="otherInGroupmsg" value="Send to Other In Group" />
        </form>
    </div>

    <div>
        <h4>To Me /specified</h4>
        <form>
            <input type="text" id="me" placeholder="Message" />
            <input type="button" id="sendToMe" value="Send to me" />
            <input type="button" id="specified" value="Send to specified connection" />
        </form>
    </div>

    <button id="stopStart" class="btn btn-info btn-small" disabled="disabled"><i class="icon-stop icon-white"></i><span>Stop Connection</span></button>

    <h5>Messages</h5>
    <ul id="messages">
    </ul>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="HubClientsAPIs.js"></script>
</asp:Content>
