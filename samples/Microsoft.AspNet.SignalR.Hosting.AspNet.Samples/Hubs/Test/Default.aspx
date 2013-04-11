<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Tests</li>
    </ul>

    <div class="page-header">
        <h2>Interesting test scenarios</h2>
        <p>End To End test scenarios that are long running and you want to see how things behave when you make live modifications on scaleout, webfarms, network connectivity</p>
    </div>

    <ul>
        <li><a href="chat.html">Chat</a> to use with scaleout, it keeps track of the webfarm node sending the message</li>
        <li><a href="clientsCaller.html">Sequential Echo</a>, good test to find duplicate and missing messages</li>
        <li><a href="clientsGroupReceiver.html">Group Receiver</a> and <a href="clientsGroupSender.html">Group Sender</a>, test messages sent between clients connected to different webfarm nodes</li>
    </ul>
    
</asp:Content>

