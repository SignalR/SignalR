// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic = signalR.transports._logic,
        // Used to prevent infinite loading icon spins in older versions of ie
        // We build this object inside a closure so we don't pollute the rest of   
        // the foreverFrame transport with unnecessary functions/utilities.
        loadPreventer = (function () {
            var loadingFixIntervalId = null,
                loadingFixInterval = 1000,
                attachedTo = 0;

            return {
                prevent: function () {
                    // Prevent additional iframe removal procedures from newer browsers
                    if (signalR._.ieVersion <= 8) {
                        // We only ever want to set the interval one time, so on the first attachedTo
                        if (attachedTo === 0) {
                            // Create and destroy iframe every 3 seconds to prevent loading icon, super hacky
                            loadingFixIntervalId = window.setInterval(function () {
                                var tempFrame = $("<iframe style='position:absolute;top:0;left:0;width:0;height:0;visibility:hidden;' src=''></iframe>");

                                $("body").append(tempFrame);
                                tempFrame.remove();
                                tempFrame = null;
                            }, loadingFixInterval);
                        }

                        attachedTo++;
                    }
                },
                cancel: function () {                   
                    // Only clear the interval if there's only one more object that the loadPreventer is attachedTo
                    if (attachedTo === 1) {
                        window.clearInterval(loadingFixIntervalId);
                    }

                    if (attachedTo > 0) {
                        attachedTo--;
                    }
                }
            };
        })();

    signalR.transports.foreverFrame = {
        name: "foreverFrame",

        supportsKeepAlive: true,

        timeOut: 3000,

        start: function (connection, onSuccess, onFailed) {
            var that = this,
                frameId = (transportLogic.foreverFrame.count += 1),
                url,
                frame = $("<iframe data-signalr-connection-id='" + connection.id + "' style='position:absolute;top:0;left:0;width:0;height:0;visibility:hidden;' src=''></iframe>");

            // Build the url
            url = transportLogic.getUrl(connection, this.name);
            url += "&frameId=" + frameId;

            if (window.EventSource && connection.host === window.location.host) {
                // If the browser supports SSE and is Same Origin, don't use Forever Frame
                if (onFailed) {
                    connection.log("This browser supports SSE, skipping Forever Frame.");
                    onFailed();
                }
                return;
            }

            // Start preventing loading icon
            // This will only perform work if the loadPreventer is not attached to another connection.
            loadPreventer.prevent();

            // Set body prior to setting URL to avoid caching issues.
            $("body").append(frame);

            frame.prop("src", url);
            transportLogic.foreverFrame.connections[frameId] = connection;

            $(window).on('message', function (e) {
                var originalEvent = e.originalEvent;

                if (originalEvent.origin !== connection.baseUrl) return;

                var data = JSON.parse(originalEvent.data);
                if (data.foreverFrameId !== frameId) return;
                that.receiveMessage(connection, data.method, data.data);
            });

            connection.frame = frame[0];
            connection.frameId = frameId;

            if (onSuccess) {
                connection.onSuccess = onSuccess;
            }

            // After connecting, if after the specified timeout there's no response stop the connection
            // and raise on failed
            window.setTimeout(function () {
                if (connection.onSuccess) {
                    connection.log("Failed to connect using forever frame source, it timed out after " + that.timeOut + "ms.");
                    that.stop(connection);

                    if (onFailed) {
                        onFailed();
                    }
                }
            }, that.timeOut);
        },

        receiveMessage: function (connection, method, data) {
            this[method](connection, data);
        },

        reconnect: function (connection) {
            var that = this;
            window.setTimeout(function () {
                if (connection.frame && transportLogic.ensureReconnectingState(connection)) {
                    var frame = connection.frame,
                        src = transportLogic.getUrl(connection, that.name, true) + "&frameId=" + connection.frameId;
                    connection.log("Updating iframe src to '" + src + "'.");
                    frame.src = src;
                }
            }, connection.reconnectDelay);
        },

        lostConnection: function (connection) {
            this.reconnect(connection);
        },

        send: function (connection, data) {
            transportLogic.ajaxSend(connection, data);
        },

        receive: function (connection, data) {
            transportLogic.processMessages(connection, data);
        },

        stop: function (connection) {
            // Stop attempting to prevent loading icon
            loadPreventer.cancel();

            if (connection.frame) {
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
            } else if (changeState(connection,
                                   signalR.connectionState.reconnecting,
                                   signalR.connectionState.connected) === true) {
                // If there's no onSuccess handler we assume this is a reconnect
                $(connection).triggerHandler(events.onReconnect);
            }
        }
    };

}(window.jQuery, window));
