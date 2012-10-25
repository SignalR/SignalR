<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.LoadTestHarness.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <title>SignalR Load Test Harness</title>
    <style>
        body { font-family: 'Segoe UI'; padding: 0 20px; margin: 0; }
        h1, h2, h3, h4, h5 { font-family: 'Segoe UI Light'; font-weight: normal; margin: 0 0 5px; }
        select, input[type=text] { margin: 2px 0 }
        #options { margin-bottom: 5px; width: 440px; padding: 6px 12px; float: left; }
        #options label { display: block; float: left; width: 180px;  }
            #options span label { display: inline; float: none; width: auto; }
        #chart {
            float: left;
            clear: right;
            margin-top: -45px;
            width: 700px;
            height: 200px;
        }
        #rate { font-size: 1.0em; }
        #rateCount, #rateSeconds { font-size: 0.8em; width: 2em; }
        #rateBatching { display: none; }
        #control { margin-top: 10px; }
    </style>
    <link href="Content/themes/base/jquery.ui.all.css" rel="stylesheet" />
</head>
<body>
    <h1>SignalR Load Test Harness</h1>
    
    <fieldset id="options"><legend>Endpoint Options</legend>
        <div>
            <label for="connectionBehavior">Connection behavior:</label>
            <select id="connectionBehavior">
                <option value="0">Listen only</option>
                <option value="1">Echo</option>
                <option value="2">Broadcast</option>
            </select>
        </div>

        <div>
            <label for="broadcastBehavior">Broadcast behavior:</label>
            <span>
                <input type="checkbox" id="broadcastBehavior" value="batching" />
                <label for="broadcastBehavior">batch</label>
            </span>
        </div>

        <div id="rate">
            <label for="rateCount">Broadcast rate:</label>
            <input id="rateCount" value="1" maxlength="3" min="1" max="100" />
            <span id="rateNoBatching">msg/sec</span>
            <span id="rateBatching">msg per <input id="rateSeconds" value="1" maxlength="3" min="1" max="10" /> sec</span>
        </div>

        <div>
            <label for="payloadSize">Broadcast size:</label>
            <select id="payloadSize">
                <option value="32">32 bytes</option>
                <option value="64">64 bytes</option>
                <option value="128">128 bytes</option>
                <option value="256">256 bytes</option>
                <option value="1024">1024 bytes</option>
                <option value="4096">4096 bytes</option>
            </select>
        </div>

        <div>
            <a id="forceGC" href="#">Force GC</a>
            <a href="LoadGenerator.html" target="_blank">Load Generator</a>
        </div>

        <div id="stats">
            <ul>
                <li>Status: <span id="status">loading...</span></li>
                <li>Server FPS: <span id="serverFps">0</span></li>
            </ul>
        </div>

        <div id="control">
            <button id="start" disabled="disabled">Start Broadcast</button>
            <button id="stop" disabled="disabled">Stop Broadcast</button>
        </div>
    </fieldset>

    <script src="Scripts/jquery-1.8.2.js"></script>
    <script src="Scripts/jquery-ui-1.9.0.js"></script>
    <script src="Scripts/jquery.signalR.js"></script>
    <script src="signalr/hubs"></script>
    <script>
        jQuery.fn.flash = function (color, duration) {
            var current = this.css("backgroundColor");
            this.animate({ backgroundColor: "rgb(" + color + ")" }, duration / 2)
                .animate({ backgroundColor: current }, duration / 2);
        };

        (function () {
            var dashboard = $.signalR.dashboard,
                $start = $("#start"),
                $stop = $("#stop"),
                $broadcastBehavior = $("#broadcastBehavior"),
                $connectionBehavior = $("#connectionBehavior"),
                $rateCount = $("#rateCount"),
                $rateSeconds = $("#rateSeconds"),
                $rateNoBatching = $("#rateNoBatching"),
                $rateBatching = $("#rateBatching"),
                $payloadSize = $("#payloadSize"),
                $status = $("#status"),
                $serverFps = $("#serverFps");

            $.extend(dashboard.client, {
                started: function () {
                    $start.prop({ disabled: true });
                    $stop.prop({ disabled: false });
                    $status.text("Running");
                },

                stopped: function () {
                    $start.prop({ disabled: false });
                    $stop.prop({ disabled: true });
                    $status.text("Stopped");
                },

                serverFps: function (fps) {
                    $serverFps.text(fps);
                },

                connectionBehaviorChanged: function (behavior) {
                    $connectionBehavior.val(behavior);
                },

                broadcastRateChanged: function (count, seconds) {
                    $rateCount.val(count);
                    $rateSeconds.val(seconds);
                },

                broadcastSizeChanged: function (size) {
                    $payloadSize.val(size);
                }
            });

            function init() {
                $connectionBehavior.change(function () {
                    dashboard.server.setConnectionBehavior($connectionBehavior.val());
                });
                $broadcastBehavior.change(function () {
                    var broadcasting = $broadcastBehavior.prop("checked");
                    if (broadcasting) {
                        $rateNoBatching.hide();
                        $rateBatching.show();
                    } else {
                        $rateBatching.hide();
                        $rateNoBatching.show();
                    }
                });
                $rateCount.spinner({
                    spin: function (e, ui) {
                        dashboard.server.setBroadcastRate(ui.value, $rateSeconds.val());
                    }
                });
                $rateSeconds.spinner({
                    spin: function (e, ui) {
                        dashboard.server.setBroadcastRate($rateCount.val(), ui.value);
                    }
                });
                $payloadSize.change(function () {
                    dashboard.server.setBroadcastSize($payloadSize.val());
                });
                $("#start").click(function (e) {
                    dashboard.server.startBroadcast();
                });
                $("#stop").click(function (e) {
                    dashboard.server.stopBroadcast();
                });
                $("#forceGC").click(function (e) {
                    /// <param name="e" type="jQuery.Event">Description</param>
                    var link = $("#forceGC"),
                        text = link.text(),
                        href = link.prop("href");

                    e.preventDefault();

                    link.text("Collecting...")
                        .prop("href", "");

                    dashboard.server.forceGC().done(function () {
                        link.text(text)
                            .prop("href", href);
                    });
                });

                dashboard.server.getStatus().done(function (status) {
                    $connectionBehavior.val(status.ConnectionBehavior);
                    $rateCount.val(status.BroadcastCount);
                    $rateSeconds.val(status.BroadcastSeconds);
                    $payloadSize.val(status.BroadcastSize);
                    $status.text(status.Broadcasting ? "Running" : "Stopped");
                    $start.prop({ disabled: status.Broadcasting });
                    $stop.prop({ disabled: !status.Broadcasting });
                });
            }

            $.signalR.hub.start(init);
        })();
    </script>
</body>
</html>