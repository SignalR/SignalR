/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic = signalR.transports._logic;

    signalR.transports.serverSentEvents = {
        name: "serverSentEvents",

        timeOut: 3000,

        start: function (connection, onSuccess, onFailed) {
            var that = this,
                opened = false,
                $connection = $(connection),
                reconnecting = !onSuccess,
                url,
                connectTimeOut;

            if (connection.eventSource) {
                connection.log("The connection already has an event source. Stopping it.");
                connection.stop();
            }

            if (!window.EventSource) {
                if (onFailed) {
                    connection.log("This browser doesn't support SSE.");
                    onFailed();
                }
                return;
            }

            $connection.trigger(events.onSending);

            url = transportLogic.getUrl(connection, this.name, reconnecting);

            try {
                connection.log("Attempting to connect to SSE endpoint '" + url + "'");
                connection.eventSource = new window.EventSource(url);
            }
            catch (e) {
                connection.log("EventSource failed trying to connect with error " + e.Message);
                if (onFailed) {
                    // The connection failed, call the failed callback
                    onFailed();
                }
                else {
                    $connection.trigger(events.onError, [e]);
                    if (reconnecting) {
                        // If we were reconnecting, rather than doing initial connect, then try reconnect again
                        that.reconnect(connection);
                    }
                }
                return;
            }

            // After connecting, if after the specified timeout there's no response stop the connection
            // and raise on failed
            connectTimeOut = window.setTimeout(function () {
                if (opened === false) {
                    connection.log("EventSource timed out trying to connect");
                    connection.log("EventSource readyState: " + connection.eventSource.readyState);

                    if (!reconnecting) {
                        that.stop(connection);
                    }

                    if (reconnecting) {
                        // If we're reconnecting and the event source is attempting to connect,
                        // don't keep retrying. This causes duplicate connections to spawn.
                        if (connection.eventSource.readyState !== window.EventSource.CONNECTING &&
                            connection.eventSource.readyState !== window.EventSource.OPEN) {
                            // If we were reconnecting, rather than doing initial connect, then try reconnect again
                            that.reconnect(connection);
                        }
                    } else if (onFailed) {
                        onFailed();
                    }
                }
            },
            that.timeOut);

            connection.eventSource.addEventListener("open", function (e) {
                connection.log("EventSource connected");

                if (connectTimeOut) {
                    window.clearTimeout(connectTimeOut);
                }

                if (opened === false) {
                    opened = true;

                    if (onSuccess) {
                        onSuccess();
                    }

                    if (reconnecting) {
                        if (changeState(connection,
                                        signalR.connectionState.reconnecting,
                                        signalR.connectionState.connected) === true) {
                            $connection.trigger(events.onReconnect);
                        }
                    }
                }
            }, false);

            connection.eventSource.addEventListener("message", function (e) {
                // process messages
                if (e.data === "initialized") {
                    return;
                }
                transportLogic.processMessages(connection, window.JSON.parse(e.data));
            }, false);

            connection.eventSource.addEventListener("error", function (e) {
                if (!opened) {
                    if (onFailed) {
                        onFailed();
                    }
                    return;
                }

                connection.log("EventSource readyState: " + connection.eventSource.readyState);

                if (e.eventPhase === window.EventSource.CLOSED) {
                    // We don't use the EventSource's native reconnect function as it
                    // doesn't allow us to change the URL when reconnecting. We need
                    // to change the URL to not include the /connect suffix, and pass
                    // the last message id we received.
                    connection.log("EventSource reconnecting due to the server connection ending");
                    that.reconnect(connection);
                } else {
                    // connection error
                    connection.log("EventSource error");
                    $connection.trigger(events.onError);
                }
            }, false);
        },

        reconnect: function (connection) {
            var that = this;
            window.setTimeout(function () {
                that.stop(connection);

                if (connection.state === signalR.connectionState.reconnecting ||
                    changeState(connection,
                                signalR.connectionState.connected,
                                signalR.connectionState.reconnecting) === true) {
                    connection.log("EventSource reconnecting");
                    that.start(connection);
                }

            }, connection.reconnectDelay);
        },

        send: function (connection, data) {
            transportLogic.ajaxSend(connection, data);
        },

        stop: function (connection) {
            if (connection && connection.eventSource) {
                connection.log("EventSource calling close()");
                connection.eventSource.close();
                connection.eventSource = null;
                delete connection.eventSource;
            }
        },
        abort: function (connection, async) {
            transportLogic.ajaxAbort(connection, async);
        }
    };

}(window.jQuery, window));