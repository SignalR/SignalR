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
        // We build this object inside a closure so we don't polute the rest of   
        // the foreverFrame transport with unnecessary functions/utilities.
        loadPreventer = (function () {
            var iframeTrashIntervalId = null,
                trashInterval = 3000,
                attachedTo = null,
                ieVersion = (function () {
                    var version,
                        matches;

                    if (window.navigator.appName === 'Microsoft Internet Explorer') {
                        // Check if the user agent has the pattern "MSIE (one or more numbers).(one or more numbers)";
                        matches = /MSIE ([0-9]+\.[0-9]+)/.exec(window.navigator.userAgent);

                        if (matches) {
                            version = window.parseFloat(matches[1]);
                        }
                    }

                    // undefined value means not IE
                    return version;
                })();

            return {
                prevent: function (connection) {
                    var garbageFrame;

                    // Prevent additional iframe trash procedures from multiple connections and newer browsers
                    if (ieVersion <= 8 && attachedTo === null) {
                        attachedTo = connection;
                        garbageFrame = $("<iframe style='position:absolute;top:0;left:0;width:0;height:0;visibility:hidden;' src=''></iframe>");

                        // Create and destroy iframe every 3 seconds to prevent loading icon, super hacky
                        iframeTrashIntervalId = window.setInterval(function () {
                            $("body").append(garbageFrame);
                            $(garbageFrame).remove();
                        }, trashInterval);
                    }
                },
                cancel: function (connection) {
                    // Only the connection that the loadPreventer is attached to can cancel the loadPreventer
                    // If a newer version of IE attachedTo will always = null so this will noop
                    if (attachedTo === connection) {
                        window.clearInterval(iframeTrashIntervalId);
                        attachedTo = null;
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

            if (window.EventSource) {
                // If the browser supports SSE, don't use Forever Frame
                if (onFailed) {
                    connection.log("This browser supports SSE, skipping Forever Frame.");
                    onFailed();
                }
                return;
            }

            // Start preventing loading icon
            // This will only perform work if the loadPreventer is not attached to another connection.
            loadPreventer.prevent(connection);

            // Build the url
            url = transportLogic.getUrl(connection, this.name);
            url += "&frameId=" + frameId;

            // Set body prior to setting URL to avoid caching issues.
            $("body").append(frame);

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

            // Stop attempting to prevent loading icon
            loadPreventer.cancel(connection);

            if (connection.frame) {
                if (connection.frame.stop) {
                    connection.frame.stop();
                } else {
                    try {
                        cw = connection.frame.contentWindow || connection.frame.contentDocument;
                        if (cw.document && cw.document.execCommand) {
                            cw.document.execCommand("Stop");
                        }
                    }
                    catch (e) {
                        connection.log("SignalR: Error occured when stopping foreverFrame transport. Message = " + e.message);
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
            } else if (changeState(connection,
                                   signalR.connectionState.reconnecting,
                                   signalR.connectionState.connected) === true) {
                // If there's no onSuccess handler we assume this is a reconnect
                $(connection).triggerHandler(events.onReconnect);
            }
        }
    };

}(window.jQuery, window));
