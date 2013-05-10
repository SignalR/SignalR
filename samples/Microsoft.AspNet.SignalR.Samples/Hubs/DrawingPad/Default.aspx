<%@ Page Title="ASP.NET SignalR: Drawing Pad" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.DrawingPad.Default" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        #pad {
            border: 2px solid #808080;
        }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Drawing Pad</li>
    </ul>

    <div class="page-header">
        <h2>Drawing Pad <small>Real-time canvas</small></h2>
        <p>An example of a collaborative drawing pad using the HTML5 canvas API.</p>
    </div>

    <div id="connecting" class="text-info">
        Please while the connection is established...
    </div>

    <div id="connected" style="display: none">
        <canvas width="700" height="400" id="pad">
            <p class="text-error">Unsupported brower</p>
        </canvas>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/Scripts/jcanvas.js") %>"></script>
    <script src="<%: ResolveUrl("~/signalr/js") %>"></script>
    <script src="DrawingPad.js"></script>
    <script>
        $(function () {
            var drawingPad = $.connection.DrawingPad;

            // someone draw something
            drawingPad.client.draw = function (value) {
                $("#pad").drawingpad("line", value);
            };

            $.connection.hub.start({ transport: activeTransport }, function () {
                drawingPad.server.join().done(function () {
                    $("#connecting").hide();
                    $("#connected").show();

                    color = drawingPad.state.color;

                    // Listen for drawing
                    $("#pad").drawingpad({ linecolor: color }).bind('line.drawingpad', function (event, args) {
                        drawingPad.server.Draw(args);
                    });
                });
            });
        });
    </script>
</asp:Content>
