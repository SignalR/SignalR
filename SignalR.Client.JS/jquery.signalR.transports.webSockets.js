/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic = signalR.transports._logic;

    signalR.transports.webSockets = {
        name: "webSockets",

        send: function (connection, data) {
            connection.socket.send(data);
        },

        start: function (connection, onSuccess, onFailed) {
            var url,
                opened = false,
                that = this,
                reconnecting = !onSuccess,
                $connection = $(connection);

            if (window.MozWebSocket) {
                window.WebSocket = window.MozWebSocket;
            }

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

                // Build the url
                $(connection).trigger(events.onSending);

                url += transportLogic.getUrl(connection, this.name, reconnecting);

                connection.log("Connecting to websocket endpoint '" + url + "'");
                connection.socket = new window.WebSocket(url);
                connection.socket.onopen = function () {
                    opened = true;
                    connection.log("Websocket opened");
                    if (onSuccess) {
                        onSuccess();
                    }
                    else {
                        if (changeState(connection,
                                        signalR.connectionState.reconnecting,
                                        signalR.connectionState.connected) === true) {
                            $connection.trigger(events.onReconnect);
                        }
                    }
                };

                connection.socket.onclose = function (event) {
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
                        $(connection).trigger(events.onError, [event.reason]);
                        connection.log("Unclean disconnect from websocket." + event.reason);
                    }
                    else {
                        connection.log("Websocket closed");
                    }

                    that.reconnect(connection);
                };

                connection.socket.onmessage = function (event) {
                    var data = window.JSON.parse(event.data),
                        $connection;
                    if (data) {
                        $connection = $(connection);

                        if (data.Messages) {
                            transportLogic.processMessages(connection, data);
                        } else {
                            $connection.trigger(events.onReceived, [data]);
                        }
                    }
                };
            }
        },

        reconnect: function (connection) {
            var that = this;
            window.setTimeout(function () {
                that.stop(connection);

                if (connection.state === signalR.connectionState.reconnecting ||
                    changeState(connection,
                                signalR.connectionState.connected,
                                signalR.connectionState.reconnecting) === true) {

                    connection.log("Websocket reconnecting");
                    that.start(connection);
                }
            },
            connection.reconnectDelay);
        },

        stop: function (connection) {
            if (connection.socket !== null) {
                connection.log("Closing the Websocket");
                connection.socket.close();
                connection.socket = null;
            }
        },

        abort: function (connection) {
        }
    };

}(window.jQuery, window));