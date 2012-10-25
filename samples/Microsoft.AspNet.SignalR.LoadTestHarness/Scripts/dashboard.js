/// <reference path="jquery-1.8.2.js" />
/// <reference path="jquery-ui-1.9.0.js" />
/// <reference path="jquery.color-2.1.0.js" />
/// <reference path="jquery.signalR.js" />
/// <reference path="knockout-2.1.0.js" />

jQuery.fn.flash = function (color, duration) {
    var current = this.css("backgroundColor");
    this.animate({ backgroundColor: "rgb(" + color + ")" }, duration / 2)
        .animate({ backgroundColor: current }, duration / 2);
};

(function ($, ko) {
    var model = $.signalR.dashboard;
    
    $.extend(model, {

        // View model
        _in: false,

        incomingNotification: function (value) {
            if (value) {
                // Set the flag
                model._in = value;
                return;
            }
            if (model._in) {
                // Pending incoming notification, set flag to false and return
                model._in = false;
                return true;
            }
            return false;
        },

        connectionBehavior: ko.observable("0"),

        batching: ko.observable(false),

        notBatching: ko.computed(function () {
            return !model.batching();
        }, model, { deferEvaluation: true }),

        broadcastCount: ko.observable(1),

        broadcastSeconds: ko.observable(1),

        broadcastSize: ko.observable(32),

        broadcasting: ko.observable(false),

        status: ko.computed(function () {
            return model.broadcasting() ? "Running" : "Stopped";
        }, model, { deferEvaluation: true }),

        serverFps: ko.observable(0),

        start: function() {
            model.server.startBroadcast();
        },

        stop: function() {
            model.server.stopBroadcast();
        },

        init: function() {
            // Hook up subscriptions to notify server when view model changes
            model.connectionBehavior.subscribe(function (newValue) {
                model.incomingNotification() || model.server.setConnectionBehavior(newValue);
            });

            model.batching.subscribe(function (newValue) {
                model.incomingNotification() || model.server.setBroadcastBehavior(newValue);
            })

            model.broadcastCount.subscribe(function (newValue) {
                model.incomingNotification() || model.server.setBroadcastRate(newValue, model.broadcastSeconds());
            })

            model.broadcastSeconds.subscribe(function (newValue) {
                model.incomingNotification() || model.server.setBroadcastRate(model.broadcastCount(), newValue);
            })

            model.broadcastSize.subscribe(function (newValue) {
                model.incomingNotification() || model.server.setBroadcastSize(newValue);
            })

            $("#rateCount").spinner({
                spin: function (e, ui) {
                    model.broadcastCount(ui.value);
                    model.server.setBroadcastRate(model.broadcastCount(), model.broadcastSeconds());
                }
            });

            $("#rateSeconds").spinner({
                spin: function (e, ui) {
                    model.broadcastSeconds(ui.value);
                    model.server.setBroadcastRate(model.broadcastCount(), model.broadcastSeconds());
                }
            });

            $("#forceGC").click(function (e) {
                /// <param name="e" type="jQuery.Event">Description</param>
                var link = $("#forceGC"),
                    text = link.text(),
                    href = link.prop("href");

                e.preventDefault();

                link.text("Collecting...")
                    .prop("href", "");

                model.server.forceGC().done(function () {
                    link.text(text)
                        .prop("href", href);
                });
            });

            // Get current dashboard status and update the view model
            model.server.getStatus().done(function (status) {
                model.incomingNotification(true);
                model.connectionBehavior(status.ConnectionBehavior);

                model.incomingNotification(true);
                model.batching(status.BroadcastBatching);

                model.incomingNotification(true);
                model.broadcastCount(status.BroadcastCount);

                model.incomingNotification(true);
                model.broadcastSeconds(status.BroadcastSeconds);

                model.incomingNotification(true);
                model.broadcastSize(status.BroadcastSize);

                model.broadcasting(status.Broadcasting);
            });
        },

        // Client hub methods
        client: {
            started: function () {
                model.broadcasting(true);
            },

            stopped: function () {
                model.broadcasting(false);
            },

            serverFps: function (fps) {
                model.serverFps(fps);
            },

            connectionBehaviorChanged: function (behavior) {
                model.incomingNotification(true);
                model.connectionBehavior(behavior);
            },

            broadcastBehaviorChanged: function (batching) {
                model.incomingNotification(true);
                model.batching(batching);
            },

            broadcastRateChanged: function (count, seconds) {
                model.incomingNotification(true);
                model.broadcastCount(count);
                model.broadcastSeconds(seconds);
            },

            broadcastSizeChanged: function (size) {
                model.incomingNotification(true);
                model.broadcastSize(size);
            }
        }
    });

    ko.applyBindings(model, document.getElementById("options"));

    $.signalR.hub.start(model.init);

})(jQuery, ko);