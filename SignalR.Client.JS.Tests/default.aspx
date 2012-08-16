<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="SignalR.Client.JS.Tests.Default" %>

<!DOCTYPE html>
<html>
    <head>
        <meta charset="utf-8">
        <title>QUnit Tests</title>
        <link rel="stylesheet" href="/QUnit/qunit.css">
    </head>
    <body>
        <form runat="server" id="main">
            <div id="qunit"></div>
            <script src="QUnit/qunit.js"></script>

            <!-- 
                Javascript is dynamically added to this panel.  First all javascript from SignalR.Client.JS is added,
                then all javascript from the Tests directory is added.  This is so that all the unit tests within the 
                Tests directory can test the SignalR.Client.JS libraries.
            -->
            <asp:Panel runat="server" ID="dynamicJavascript">

            </asp:Panel>
        </form>
    </body>
</html>


