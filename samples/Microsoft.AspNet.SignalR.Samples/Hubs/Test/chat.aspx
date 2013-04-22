<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li><a href="default.aspx">Scenarios</a> <span class="divider">/</span></li>
        <li class="active">Chat</li>
    </ul>    
    <table>
        <tr>
            <td>Try other transports:</td>
            <td><a href="chat.aspx?transport=webSockets">webSockets</a></td>
            <td><a href="chat.aspx?transport=foreverFrame">foreverFrame</a></td>
            <td><a href="chat.aspx?transport=serverSentEvents">serverSentEvents</a></td>
            <td><a href="chat.aspx?transport=longPolling">longPolling</a></td>
        </tr>
    </table>
    <table>
        <tr>
            <td>Connections</td>
            <td>Groups</td>
            <td>Joined Groups</td>
            <td>Received Messages</td>
        </tr>
        <tr>
            <td><select id="ConnectionsList" style="width:300px" size="10"></select></td>
            <td><select id="GroupsList" style="width:200px" size="10"></select></td>
            <td><select id="JoinedGroupsList" style="width:200px" size="10"></select></td>
            <td><textarea id="ReceivedTextArea" style="width:600px; height:150px"></textarea></td>
        </tr>
        <tr>
            <td colspan="4">
                <label>ConnectionId:</label>
                <input id="ConnectionText" type="text" />
                <label>Group:</label>
                <input id="GroupText" type="text" />
                <label>Message:</label>
                <input id="MessageText" type="text" size="100" />                
            </td>
        </tr>
        <tr>
            <td colspan="4">
                <button id="JoinGroupButton">Join Group</button>
                <button id="LeaveGroupButton">Leave Group</button>
                <button id="SendToAllButton">Send To All</button>
                <button id="SendToCallerButton">Send To Caller</button>
                <button id="SendToClientButton">Send To Client</button>
                <button id="SendToGroupButton">Send To Group</button>
            </td>
        </tr>   
    </table>
    <h1>Client Validation</h1>
    <ul id="ClientMessages">
    </ul>
    <h1>TestHub</h1>    
    <ul id="HubMessages">
    </ul>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="common.js"></script>
    <script src="chat.js"></script>
</asp:Content>

