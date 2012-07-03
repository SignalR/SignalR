/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic = signalR.transports._logic;

    signalR.transports.foreverFrame = {
        name: "foreverFrame",

        timeOut: 3000,

        start: function (connection, onSuccess, onFailed) {
            var that = this,
                frameId = (transportLogic.foreverFrame.count += 1),
                url,
                connectTimeOut,
                frame = $("<iframe data-signalr-connection-id='" + connection.id + "' style='position:absolute;top:0;left:0;width:0;height:0;visibility:hidden;'></iframe>");

            if (window.EventSource) {
                // If the browser supports SSE, don't use Forever Frame
                if (onFailed) {
                    connection.log("This brower supports SSE, skipping Forever Frame.");
                    onFailed();
                }
                return;
            }

            $(connection).trigger(events.onSending);

            // Build the url
            url = transportLogic.getUrl(connection, this.name);
            url += "&frameId=" + frameId;

            frame.prop("src", url);
            transportLogic.foreverFrame.connections[frameId] = connection;

            connection.log("Binding to iframe's readystatechange event.");
            frame.bind("readystatechange", function () {
                if ($.inArray(this.readyState, ["loaded", "complete"]) >= 0) {
                    connection.log("Forever frame iframe readyState changed to " + this.readyState + ", reconnecting");

                    that.reconnect(connection);
                }
            });

            connection.frame = frame[0];
            connection.frameId = frameId;

            if (onSuccess) {
                connection.onSuccess = onSuccess;
            }

            $("body").append(frame);

            // After connecting, if after the specified timeout there's no response stop the connection
            // and raise on failed
            // REVIEW: Why is connectTimeOut set here and never used again?
            connectTimeOut = window.setTimeout(function () {
                if (connection.onSuccess) {
                    connection.log("Failed to connect using forever frame source, it timed out after " + that.timeOut + "ms.");
                    that.stop(connection);

                    if (onFailed) {
                        onFailed();
                    }
                }
            }, that.timeOut);
        },

        reconnect: function (connection) {
            var that = this;
            window.setTimeout(function () {
                if (!connection.frame) {
                    return;
                }

                if (connection.state === signalR.connectionState.reconnecting ||
                    changeState(connection,
                                signalR.connectionState.connected,
                                signalR.connectionState.reconnecting) === true) {

                    var frame = connection.frame,
                    src = transportLogic.getUrl(connection, that.name, true) + "&frameId=" + connection.frameId;
                    connection.log("Upating iframe src to '" + src + "'.");
                    frame.src = src;
                }

            }, connection.reconnectDelay);
        },

        send: function (connection, data) {
            transportLogic.ajaxSend(connection, data);
        },

        receive: function (connection, data) {
            var cw;
            transportLogic.processMessages(connection, data);
            // Delete the script & div elements
            connection.frameMessageCount = (connection.frameMessageCount || 0) + 1;
            if (connection.frameMessageCount > 50) {
                connection.frameMessageCount = 0;
                cw = connection.frame.contentWindow || connection.frame.contentDocument;
                if (cw && cw.document) {
                    $("body", cw.document).empty();
                }
            }
        },

        stop: function (connection) {
            var cw = null;
            if (connection.frame) {
                if (connection.frame.stop) {
                    connection.frame.stop();
                } else {
                    cw = connection.frame.contentWindow || connection.frame.contentDocument;
                    if (cw.document && cw.document.execCommand) {
                        cw.document.execCommand("Stop");
                    }
                }
                $(connection.frame).remove();
                delete transportLogic.foreverFrame.connections[connection.frameId];
                connection.frame = null;
                connection.frameId = null;
                delete connection.frame;
                delete connection.frameId;
                connection.log("Stopping forever frame");
            }
        },

        abort: function (connection, async) {
            transportLogic.ajaxAbort(connection, async);
        },

        getConnection: function (id) {
            return transportLogic.foreverFrame.connections[id];
        },

        started: function (connection) {
            if (connection.onSuccess) {
                connection.onSuccess();
                connection.onSuccess = null;
                delete connection.onSuccess;
            }
            else {
                if (changeState(connection,
                                signalR.connectionState.reconnecting,
                                signalR.connectionState.connected) === true) {
                    // If there's no onSuccess handler we assume this is a reconnect
                    $(connection).trigger(events.onReconnect);
                }
            }
        }
    };

}(window.jQuery, window));