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

    function ViewModel(dashboard) {
        var self = this;
        
        this.hub = dashboard;
        
        this._in = false;

        this.incomingNotification = function (value) {
            if (value) {
                // Set the flag
                self._in = value;
                return;
            }
            if (self._in) {
                // Pending incoming notification, set flag to false and return
                self._in = false;
                return true;
            }
            return false;
        };

        this.connectionBehavior = ko.observable(0);

        this.batching = ko.observable(false);

        this.notBatching = ko.computed(function () {
            return !self.batching();
        });

        this.broadcastCount = ko.observable(1);

        this.broadcastSeconds = ko.observable(1);

        this.broadcastSize = ko.observable(32);

        this.broadcasting = ko.observable(false);

        this.status = ko.computed(function () {
            return self.broadcasting() ? "Running" : "Stopped";
        });

        this.serverFps = ko.observable(0);

        this.GCStatus = ko.observable("Force GC");

        this.GCRunning = ko.observable(false);

        this.forceGC = function () {
            self.GCStatus("Collecting...");
            self.GCRunning(true);

            self.hub.server.forceGC().done(function () {
                self.GCStatus("Force GC");
                self.GCRunning(true);
            });
        };

        this.start = function() {
            self.hub.server.startBroadcast();
        };

        this.stop = function() {
            self.hub.server.stopBroadcast();
        };

        this.init = function() {

            // Hook up subscriptions to notify server when view model changes
            self.connectionBehavior.subscribe(function (newValue) {
                self.incomingNotification() || self.hub.server.setConnectionBehavior(newValue);
            });

            self.batching.subscribe(function (newValue) {
                self.incomingNotification() || self.hub.server.setBroadcastBehavior(newValue);
            })

            self.broadcastCount.subscribe(function (newValue) {
                self.incomingNotification() || self.hub.server.setBroadcastRate(newValue, self.broadcastSeconds());
            })

            self.broadcastSeconds.subscribe(function (newValue) {
                self.incomingNotification() || self.hub.server.setBroadcastRate(self.broadcastCount(), newValue);
            })

            self.broadcastSize.subscribe(function (newValue) {
                self.incomingNotification() || self.hub.server.setBroadcastSize(newValue);
            })

            $("#rateCount").spinner({
                spin: function (e, ui) {
                    self.broadcastCount(ui.value);
                }
            });

            $("#rateSeconds").spinner({
                spin: function (e, ui) {
                    self.broadcastSeconds(ui.value);
                }
            });

            // Get current dashboard status and update the view model
            self.hub.server.getStatus().done(function (status) {
                self.incomingNotification(true);
                self.connectionBehavior(status.ConnectionBehavior);

                self.incomingNotification(true);
                self.batching(status.BroadcastBatching);

                self.incomingNotification(true);
                self.broadcastCount(status.BroadcastCount);

                self.incomingNotification(true);
                self.broadcastSeconds(status.BroadcastSeconds);

                self.incomingNotification(true);
                self.broadcastSize(status.BroadcastSize);

                self.broadcasting(status.Broadcasting);
            });
        }
    }

    var dashboard = $.signalR.dashboard,
        model;

    $.extend(dashboard.client, {
        // Client hub methods
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
            model.incomingNotification(true);
            model.broadcastSeconds(seconds);
        },

        broadcastSizeChanged: function (size) {
            model.incomingNotification(true);
            model.broadcastSize(size);
        }
    });
    
    model = new ViewModel(dashboard);
    
    ko.applyBindings(model, document.getElementById("options"));

    $.signalR.hub.start(model.init);

})(jQuery, ko);