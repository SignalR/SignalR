/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jquery.signalR.js" />
/// <reference path="../../Scripts/hubs.js" />

$(function () {
    "use strict";

    var hub = $.connection.realtime,
        $state = $("#state"),
        $interval = $("#interval"),
        $msgRate = $("#msgRate"),
        running = false,
        interval = 1000 / 25,
        tickId = -1,
        msgCount = 0,
        time = new Date();

    function measure() {
        var $msgRates,
            msgRate,
            expected = 1000 / interval,
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
            tickId = 0;
            running = false;
        },

        intervalChanged: function (newInterval) {
            interval = newInterval;
            $interval.val(newInterval);
        },

        tick: function (id) {
            var $msgRates = $msgRate.children("li");
            if (tickId >= 0 && id !== tickId + 1) {
                if ($msgRates.length === 0) {
                    $msgRate.append("<li>Missed a message!</li>");
                } else {
                    $msgRates.first().before("<li>Missed a message!</li>");
                }
            }
            tickId += 1;
            msgCount += 1;
        }
    });

    $.connection.hub.start().done(function () {
        hub.server.getTickId().done(function (result) {
            tickId = result;
        });
        hub.server.isEngineRunning().done(function (result) {
            result ? hub.client.engineStarted() : hub.client.engineStopped();
        });
        hub.server.getInterval().done(hub.client.intervalChanged);

        $("#start").click(function () {
            hub.server.start();
        });

        $("#stop").click(function () {
            hub.server.stop();
        });
    });
});