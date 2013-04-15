<%@ Page  Title="ASP.NET SignalR: HubCleints APIs Demo" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.HubClientsAPIs.Default" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">HubCleints APIs</li>
    </ul>

    <div class="page-header">
        <h2>Hub Clients APIs </h2>
        <p>Demonstrates Hub Clients APIs including starting and stopping, sending and receiving messages, and managing groups.
        </p>
    </div>
           
    <label for="user">
        Group:
    </label>
    <input type="text" name="group" id="group" placeholder="Type a group" />
    <input type="button" id="join-group" value="Join Group" />
    <input type="button" id="leave-group" value="Leave Group" />
     
    <br />   
    <label for="user">
        Specified ConnectionID:
    </label>
    <input type="text"  name="connection" id="connection" />
     
    <h4>
        To Everybody</h4>
    <form>
    <input type="text" id="msg"  placeholder="Type a message"/>
    <input type="button" id="broadcast" value="Broadcast" />
    <input type="button" id="broadcast-except-specified" value="Broadcast (All Except specified Connection)" />
    <input type="button" id="other" value="Other (Everyone but me)" />    
    </form>
        
    <h4>
        To Group</h4>
    <form>
    <input type="text" name="message" id="groupMessage" placeholder="Type a message" /> 
    <input type="button" id="groupmsg" value="Send to group" />
    <input type="button" id="otherInGroupmsg" value="Send to Other In Group" />
    </form>

    <h4>
        To Me /specified</h4>
    <form>
    <input type="text" id="me" placeholder="Type a message" />
    <input type="button" id="sendToMe" value="Send to me" />
    <input type="button" id="specified" value="Send specified connection" />
    </form>
    
    <button id="stopStart" class="btn btn-info btn-small" disabled="disabled"><i class="icon-stop icon-white"></i> <span>Stop Connection</span></button>
            
    <ul id="messages">
    </ul>
   
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="HubClientsAPIs.js"></script>
</asp:Content>
