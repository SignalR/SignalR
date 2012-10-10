<%@ Page Title="SignalR Samples" Language="C#" MasterPageFile="~/SignalR.Samples.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="SignalR.Samples._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    
    <div id="home">
        <ul>
            <li class="raw"><a href="Raw/"><p class="title">Raw Connection</p></a></li>
            <li class="streaming"><a href="Streaming/"><p class="title">Streaming</p></a></li>
            <li class="demo-hub"><a href="Hubs/DemoHub/"><p class="title">Demo Hub</p></a></li>
            <li class="status"><a href="Hubs/ConnectDisconnect/"><p class="title">Connection Status</p></a></li>
            <li class="chat"><a href="Hubs/Chat/"><p class="title">Chat</p></a></li>
            <li class="mouse-tracking"><a href="Hubs/MouseTracking/"><p class="title">Mouse Tracking</p></a></li>
            <li class="drawing-pad"><a href="Hubs/DrawingPad"><p class="title">Drawing pad</p></a></li>
            <li class="shape-share"><a href="Hubs/ShapeShare/"><p class="title">Shape Share</p></a></li>
            <li class="hub-bench"><a href="Hubs/Benchmark/"><p class="title">Hub Ping</p></a></li>
            <li class="hub-auth"><a href="Hubs/Auth/"><p class="title">Hub Auth</p></a></li>
        </ul>
    </div>
    <div class="clear"></div>
    <script src="Scripts/jquery-1.6.2.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.color.js" type="text/javascript"></script>
    <script src="Scripts/jquery.transform.js" type="text/javascript"></script>
    <script src="Scripts/jquery.easing.1.3.js" type="text/javascript"></script>
    <script src="Scripts/jquery.hoverMorph.js" type="text/javascript"></script>
    <script>
        $(function () {
            $("#home li").hoverMorph()
                .click(function (e) {
                    if (e.target.tagName.toLowerCase() !== "a") {
                        document.location = $(this).find("a").attr("href");
                    }
                })
                .css({
                    cursor: "pointer"
                });
        });
    </script>
</asp:Content>
