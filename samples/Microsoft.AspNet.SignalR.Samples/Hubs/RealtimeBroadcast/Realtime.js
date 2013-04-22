/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/jquery.signalR.js" />
/// <reference path="../../Scripts/hubs.js" />

$(function () {
    "use strict";

    var hub = $.connection.realtime,
        $state = $("#state"),
        $fps = $("#fps"),
        $serverFps = $("#serverFps"),
        $msgRate = $("#msgRate"),
        running = false,
        fps = 25,
        frameId = -1,
        msgCount = 0,
        time = new Date();

    function measure() {
        var $msgRates,
            msgRate,
            expected = fps,
            diff = msgCount - expected,
            t = new Date() - time;

        if (running) {
            $msgRates = $msgRate.children("li");

            if ($msgRates.length === 5) {
                $msgRates.last().remove();
            }

            msgRate = "<li>" + msgCount + " (" + diff + ", " + t + "ms)</li>";

            if ($msgRates.length === 0) {
                $msgRate.append(msgRate);
            } else {
                $msgRates.first().before(msgRate);
            }
        }

        msgCount = 0;
        time = new Date();
        window.setTimeout(measure, 1000);
    }

    window.setTimeout(measure, 1000);

    $.extend(hub.client, {
        engineStarted: function () {
            running = true;
            $state.html("Running");
        },

        engineStopped: function () {
            $state.html("Stopped");
            frameId = 0;
            running = false;
        },

        fpsChanged: function (newFps) {
            fps = newFps;
            $fps.val(newFps);
        },

        serverFps: function (fps) {
            $serverFps.val(fps);
        },

        frame: function (id) {
            var $msgRates = $msgRate.children("li");
            if (frameId >= 0 && id !== frameId + 1) {
                if ($msgRates.length === 0) {
                    $msgRate.append("<li>Missed a message!</li>");
                } else {
                    $msgRates.first().before("<li>Missed a message!</li>");
                }
            }
            frameId += 1;
            msgCount += 1;
        }
    });

    $.connection.hub.start().done(function () {
        hub.server.getFrameId().done(function (result) {
            frameId = result;
        });
        hub.server.isEngineRunning().done(function (result) {
            result ? hub.client.engineStarted() : hub.client.engineStopped();
        });
        hub.server.getFPS().done(hub.client.fpsChanged);

        $("#start").click(function () {
            hub.server.start();
        });

        $("#stop").click(function () {
            hub.server.stop();
        });

        $fps.change(function () {
            var newFps = $fps.val();
            fps = newFps;
            hub.server.setFPS(newFps);
        });
    });
});