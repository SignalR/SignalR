<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.LoadTestHarness.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <title>SignalR Load Test Harness</title>
    <link href="Content/themes/base/jquery.ui.all.css" rel="stylesheet" />
    <link href="Content/dashboard.css" rel="stylesheet" />
</head>
<body>
    <h1>ASP.NET SignalR Load Test Harness</h1>
    
    <fieldset id="options"><legend>Dashboard</legend>
        <div>
            <label for="connectionBehavior">Connection behavior:</label>
            <select id="connectionBehavior" data-bind="value: connectionBehavior">
                <option value="0">Listen only</option>
                <option value="1">Echo</option>
                <option value="2">Broadcast</option>
            </select>
        </div>

        <div>
            <label for="broadcastBehavior">Broadcast behavior:</label>
            <span>
                <input type="checkbox" id="broadcastBehavior" data-bind="checked: batching" />
                <label for="broadcastBehavior">batch</label>
            </span>
        </div>

        <div id="rate">
            <label for="rateCount">Broadcast rate:</label>
            <input id="rateCount" maxlength="3" min="1" max="100" data-bind="value: broadcastCount" />
            <span data-bind="visible: notBatching">msg/sec</span>
            <span data-bind="visible: batching">msg per <input id="rateSeconds" maxlength="3" min="1" max="10"  data-bind="value: broadcastSeconds" /> sec</span>
        </div>

        <div>
            <label for="payloadSize">Broadcast size:</label>
            <select id="payloadSize" data-bind="value: broadcastSize">
                <option value="32">32 bytes</option>
                <option value="64">64 bytes</option>
                <option value="128">128 bytes</option>
                <option value="256">256 bytes</option>
                <option value="1024">1024 bytes</option>
                <option value="4096">4096 bytes</option>
            </select>
        </div>

        <div>
            <a id="forceGC" href="#" data-bind="text: GCStatus, disable: GCRunning,  click: forceGC">Force GC</a>
            <a href="LoadGenerator.html" target="_blank">Load Generator</a>
        </div>

        <div id="stats">
            <ul>
                <li>Status: <span id="status" data-bind="text: status"></span></li>
                <li>Server FPS: <span id="serverFps" data-bind="text: serverFps"></span></li>
            </ul>
        </div>

        <div id="control">
            <button id="start" data-bind="disable: broadcasting, click: start">Start Broadcast</button>
            <button id="stop" data-bind="enable: broadcasting, click: stop">Stop Broadcast</button>
        </div>
    </fieldset>

    <script src="Scripts/jquery-1.8.2.js"></script>
    <script src="Scripts/jquery-ui-1.9.0.js"></script>
    <script src="Scripts/jquery.color-2.1.0.js"></script>
    <script src="Scripts/jquery.signalR.js"></script>
    <script src="signalr/js"></script>
    <script src="Scripts/knockout-2.1.0.debug.js"></script>
    <script src="Scripts/dashboard.js"></script>
</body>
</html>