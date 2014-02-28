<%@ Page Title="SignalR Auth Sample" Language="C#" MasterPageFile="~/SignalR.Master"  AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.Auth._Default" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Hub Authorization</li>
    </ul>

    <div class="page-header">
        <h2>Hub Authorization <small>Protect hubs and hub methods</small></h2>
        <p>Demonstrates how to use the authorization features of the Hub API to restrict certain Hubs and methods to specific users.</p>
    </div>

    <form runat="server" class="form-inline">
        <asp:Label runat="server" AssociatedControlID="userName" Text="User Name:" />
        <asp:TextBox ID="userName" runat="server" placeholder="Enter 'User'" />
        
        <asp:Label runat="server" AssociatedControlID="roles" Text="Roles:" />
        <asp:TextBox ID ="roles" runat="server" placeholder="Enter 'Invoker' or 'Admin'" />
        
        <asp:Button runat="server" ID="login" CssClass="btn" Text="Log in" OnClick="Login" />
    </form>

    <div id="messages">
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/js") %>"></script>
    <script src="Auth.js"></script>
</asp:Content>
