/*global window:false */
/// <reference path="jquery.signalR.core.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        keepAliveInterval,
        keepAliveTimeout,
        lastKeepAlivePinged;

    signalR.transports = {};

    function checkIfAlive(connection) {
        // Only check if we're alive (if we're connected)
        if (connection.state === signalR.connectionState.connected) {
            var diff = new Date();

            diff.setTime(diff - lastKeepAlivePinged);

            // Check if the keep alive has timed out
            if (diff.getTime() >= keepAliveTimeout) {
                window.console.log("Keep alive timed out, shifting to reconnecting.");

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

            if (connection.transport.supportsKeepAlive) {
                window.console.log("Pinging via message received.");
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
            // If we haven't initiated the keep alive timeouts then we need to set them up
            if (!keepAliveTimeout) {
                window.console.log("Now monitoring Keep alive");
                // Converting the timeout to millseconds
                keepAliveTimeout = connection.keepAliveTimeout * 1000;
                window.console.log("Keep alive timeout: " + keepAliveTimeout);

                // Set the ping time tracker
                window.console.log("Pinging via monitorKeepAlive.");
                this.pingKeepAlive(connection);

                // Initiate interval to check timeouts
                keepAliveInterval = window.setInterval(function () {
                    checkIfAlive(connection);
                }, keepAliveTimeout);
            }
            else {
                window.console.log("Tried to monitor keep alive but it's already being monitored");
            }
        },

        pingKeepAlive: function (connection) {
            lastKeepAlivePinged = new Date();
        },

        foreverFrame: {
            count: 0,
            connections: {}
        }
    };

}(window.jQuery, window));