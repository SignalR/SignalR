<%@ Page Title="SignalR Auth Sample" Language="C#" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="SignalR.Hosting.AspNet.Samples.Hubs.Auth._Default" %>
<!DOCTYPE html>
<html>
<head>
    <title>Auth Sample</title>
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <script src="../../Scripts/signalr.samples.js" type="text/javascript"></script>
    <script src="../../Scripts/jquery-1.6.2.min.js" type="text/javascript"></script>
    <script src="../../Scripts/json2.min.js" type="text/javascript"></script>
    <script src="../../Scripts/jquery.signalR.js" type="text/javascript"></script>
    <script src="../../signalr/hubs" type="text/javascript"></script>
    <script src="Auth.js" type="text/javascript"></script>
</head>
<body>
    <form runat="server">
        <asp:Label ID="userNameLabel" AssociatedControlID="userName" Text="User Name: " runat="server" />
        <asp:TextBox ID="userName" runat="server" />
        <asp:Label ID="rolesLabel" AssociatedControlID="roles" Text="Roles: " runat="server" />
        <asp:TextBox ID ="roles" runat="server" />
        <asp:Button ID="login" runat="server" Text="login" OnClick="Login" />
    </form>
</body>
</html>
