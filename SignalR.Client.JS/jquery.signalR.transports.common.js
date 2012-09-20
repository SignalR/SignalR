/*global window:false */
/// <reference path="jquery.signalR.core.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events;

    signalR.transports = {};

    function checkIfAlive(connection) {
        // Only check if we're connected
        if (connection.state === signalR.connectionState.connected) {
            var keepAliveData = connection.keepAliveData,
                diff = new Date();

            diff.setTime(diff - keepAliveData.lastPinged);

            // Check if the keep alive has timed out
            if (diff.getTime() >= keepAliveData.timeout) {
                connection.log("Keep alive timed out");

                // Notify transport that the connection has been lost
                connection.transport.lostConnection(connection);
            }
        }
    }

    signalR.transports._logic = {
        addQs: function (url, connection) {
            if (!connection.qs) {
                return url;
            }

            if (typeof (connection.qs) === "object") {
                return url + "&" + $.param(connection.qs);
            }

            if (typeof (connection.qs) === "string") {
                return url + "&" + connection.qs;
            }

            return url + "&" + window.escape(connection.qs.toString());
        },

        getUrl: function (connection, transport, reconnecting, appendReconnectUrl) {
            /// <summary>Gets the url for making a GET based connect request</summary>
            var baseUrl = transport === "webSockets" ? "" : connection.baseUrl,
                url = baseUrl + connection.appRelativeUrl,
                qs = "transport=" + transport + "&connectionId=" + window.escape(connection.id);

            if (connection.data) {
                qs += "&connectionData=" + window.escape(connection.data);
            }

            if (!reconnecting) {
                url = url + "/connect";
            } else {
                if (appendReconnectUrl) {
                    url = url + "/reconnect";
                }
                if (connection.messageId) {
                    qs += "&messageId=" + connection.messageId;
                }
                if (connection.groups) {
                    qs += "&groups=" + window.escape(JSON.stringify(connection.groups));
                }
            }
            url += "?" + qs;
            url = this.addQs(url, connection);
            url += "&tid=" + Math.floor(Math.random() * 11);
            return url;
        },

        ajaxSend: function (connection, data) {
            var url = connection.url + "/send" + "?transport=" + connection.transport.name + "&connectionId=" + window.escape(connection.id);
            url = this.addQs(url, connection);
            return $.ajax({
                url: url,
                global: false,
                type: connection.ajaxDataType === "jsonp" ? "GET" : "POST",
                dataType: connection.ajaxDataType,
                data: {
                    data: data
                },
                success: function (result) {
                    if (result) {
                        $(connection).trigger(events.onReceived, [result]);
                    }
                },
                error: function (errData, textStatus) {
                    if (textStatus === "abort" ||
                        (textStatus === "parsererror" && connection.ajaxDataType === "jsonp")) {
                        // The parsererror happens for sends that don't return any data, and hence
                        // don't write the jsonp callback to the response. This is harder to fix on the server
                        // so just hack around it on the client for now.
                        return;
                    }
                    $(connection).trigger(events.onError, [errData]);
                }
            });
        },

        ajaxAbort: function (connection, async) {
            if (typeof (connection.transport) === "undefined") {
                return;
            }

            // Async by default unless explicitly overidden
            async = typeof async === "undefined" ? true : async;

            var url = connection.url + "/abort" + "?transport=" + connection.transport.name + "&connectionId=" + window.escape(connection.id);
            url = this.addQs(url, connection);
            $.ajax({
                url: url,
                async: async,
                timeout: 1000,
                global: false,
                type: "POST",
                dataType: connection.ajaxDataType,
                data: {}
            });

            connection.log("Fired ajax abort async = " + async);
        },

        processMessages: function (connection, data) {
            var $connection = $(connection);

            // If our transport supports keep alive then we need to update the ping time stamp.
            if (connection.transport.supportsKeepAlive) {
                this.pingKeepAlive(connection);
            }

            if (!data) {
                return;
            }

            if (data.Disconnect) {
                connection.log("Disconnect command received from server");

                // Disconnected by the server
                connection.stop();
                return;
            }

            if (data.Messages) {
                $.each(data.Messages, function () {
                    try {
                        $connection.trigger(events.onReceived, [this]);
                    }
                    catch (e) {
                        connection.log("Error raising received " + e);
                        $(connection).trigger(events.onError, [e]);
                    }
                });
            }

            if (data.MessageId) {
                connection.messageId = data.MessageId;
            }

            if (data.TransportData) {
                connection.groups = data.TransportData.Groups;
            }
        },

        monitorKeepAlive: function (connection) {
            var keepAliveData = connection.keepAliveData;

            // If we haven't initiated the keep alive timeouts then we need to
            if (!keepAliveData.keepAliveCheckIntervalID) {
               
                // Initialize the keep alive time stamp ping
                this.pingKeepAlive(connection);

                // Initiate interval to check timeouts
                keepAliveData.keepAliveCheckIntervalID = window.setInterval(function () {
                    checkIfAlive(connection);
                }, keepAliveData.timeout);

                connection.log("Now monitoring keep alive with timeout of: " + keepAliveData.timeout);
            }
            else {
                connection.log("Tried to monitor keep alive but it's already being monitored");
            }
        },

        stopMonitoringKeepAlive: function (connection) {
            var keepAliveInterval = connection.keepAliveData.keepAliveCheckIntervalID;

            // Only attempt to stop the keep alive monitoring if its being monitored
            if (keepAliveInterval) {
                // Stop the interval
                window.clearInterval(keepAliveInterval);

                // Clear all the keep alive data
                connection.keepAliveData = {};
                connection.log("Stopping the monitoring of the keep alive");
            }
        },

        pingKeepAlive: function (connection) {
            connection.keepAliveData.lastPinged = new Date();
        },

        foreverFrame: {
            count: 0,
            connections: {}
        }
    };

}(window.jQuery, window));