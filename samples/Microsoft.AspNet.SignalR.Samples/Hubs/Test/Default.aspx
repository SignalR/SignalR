<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Scenarios</li>
    </ul>

    <div class="page-header">
        <h2>Test Scenarios</h2>
        <p>Long running scenarios to execute while doing live modifications with scaleout, webfarms, network connectivity</p>
    </div>

    <ul>
        <li><a href="chat.aspx">Chat</a> to use with scaleout, it keeps track of the webfarm node sending the message</li>
        <li><a href="clientsCaller.aspx">Sequential Echo</a>, good test to find duplicate and missing messages</li>
        <li><a href="clientsGroupReceiver.aspx">Group Receiver</a> and <a href="clientsGroupSender.aspx">Group Sender</a>, test messages sent between clients connected to different webfarm nodes</li>
    </ul>
    
</asp:Content>

