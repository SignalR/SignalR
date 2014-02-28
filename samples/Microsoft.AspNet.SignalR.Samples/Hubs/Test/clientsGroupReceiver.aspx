﻿<%@ Page Title="ASP.NET SignalR: Connection Status" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li><a href="default.aspx">Scenarios</a> <span class="divider">/</span></li>
        <li class="active">Group Receiver</li>
    </ul>
    <table>
        <tr>
            <td>Try other transports:</td>
            <td><a href="clientsGroupReceiver.aspx?transport=webSockets">webSockets</a></td>
            <td><a href="clientsGroupReceiver.aspx?transport=foreverFrame">foreverFrame</a></td>
            <td><a href="clientsGroupReceiver.aspx?transport=serverSentEvents">serverSentEvents</a></td>
            <td><a href="clientsGroupReceiver.aspx?transport=longPolling">longPolling</a></td>
        </tr>
    </table>   
    <table>
        <tr>
            <td>Received: <label id="ReceivedLabel">0</label></td>
        </tr> 
    </table>
    <h1>TestHub</h1>    
    <ul id="HubMessages">
    </ul>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="common.js"></script>
    <script src="clientsGroupReceiver.js"></script>
</asp:Content>