// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window, undefined) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic = signalR.transports._logic;

    signalR.transports.webSockets = {
        name: "webSockets",

        supportsKeepAlive: true,

        timeOut: 3000,

        send: function (connection, data) {
            connection.socket.send(data);
        },

        start: function (connection, onSuccess, onFailed) {
            var url,
                opened = false,
                that = this,
                initialSocket,
                timeOutHandle,
                reconnecting = !onSuccess;

            if (!window.WebSocket) {
                onFailed();
                return;
            }

            if (!connection.socket) {
                if (connection.webSocketServerUrl) {
                    url = connection.webSocketServerUrl;
                }
                else {
                    url = connection.wsProtocol + connection.host;
                }

                url += transportLogic.getUrl(connection, this.name, reconnecting);

                connection.log("Connecting to websocket endpoint '" + url + "'.");
                connection.socket = new window.WebSocket(url);

                // Issue #1653: Galaxy S3 Android Stock Browser fails silently to establish websocket connections. 
                if (onFailed) {
                    initialSocket = connection.socket;
                    timeOutHandle = window.setTimeout(function () {
                        if (initialSocket === connection.socket) {
                            connection.log("WebSocket timed out trying to connect.");
                            onFailed();
                        }
                    }, that.timeOut);
                }

                connection.socket.onopen = function () {
                    window.clearTimeout(timeOutHandle);
                    opened = true;
                    connection.log("Websocket opened.");
                };

                connection.socket.onclose = function (event) {
                    // Only handle a socket close if the close is from the current socket.
                    // Sometimes on disconnect the server will push down an onclose event
                    // to an expired socket.
                    window.clearTimeout(timeOutHandle);

                    if (this === connection.socket) {
                        if (!opened) {
                            if (onFailed) {
                                onFailed();
                            }
                            else if (reconnecting) {
                                that.reconnect(connection);
                            }
                            return;
                        }
                        else if (typeof event.wasClean !== "undefined" && event.wasClean === false) {
                            // Ideally this would use the websocket.onerror handler (rather than checking wasClean in onclose) but
                            // I found in some circumstances Chrome won't call onerror. This implementation seems to work on all browsers.
                            $(connection).triggerHandler(events.onError, [event.reason]);
                            connection.log("Unclean disconnect from websocket: " + event.reason || "[no reason given].");
                        }
                        else {
                            connection.log("Websocket closed.");
                        }

                        that.reconnect(connection);
                    }
                };

                connection.socket.onmessage = function (event) {
                    var data,
                        $connection = $(connection);

                    try {
                        data = connection._parseResponse(event.data);
                    }
                    catch (error) {
                        transportLogic.handleParseFailure(connection, event.data, error.message, onFailed);
                        return;
                    }
                    
                    if (onSuccess) {
                        onSuccess();
                        onSuccess = null;
                    } else if (changeState(connection,
                                         signalR.connectionState.reconnecting,
                                         signalR.connectionState.connected) === true) {
                        $connection.triggerHandler(events.onReconnect);
                    }

                    transportLogic.clearReconnectTimeout(connection);

                    if (data) {
                        // data.M is PersistentResponse.Messages
                        if ($.isEmptyObject(data) || data.M) {
                            transportLogic.processMessages(connection, data);
                        } else {
                            // For websockets we need to trigger onReceived
                            // for callbacks to outgoing hub calls.
                            $connection.triggerHandler(events.onReceived, [data]);
                        }
                    }
                };
            }
        },

        reconnect: function (connection) {
            transportLogic.reconnect(connection, this.name);
        },

        lostConnection: function (connection) {
            this.reconnect(connection);

        },

        stop: function (connection) {
            // Don't trigger a reconnect after stopping
            transportLogic.clearReconnectTimeout(connection);

            if (connection.socket !== null) {
                connection.log("Closing the Websocket.");
                connection.socket.close();
                connection.socket = null;
            }
        },

        abort: function (connection, async) {
            transportLogic.ajaxAbort(connection, async);
        }
    };

}(window.jQuery, window));
