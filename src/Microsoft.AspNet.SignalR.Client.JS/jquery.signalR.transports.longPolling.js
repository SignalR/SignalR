// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        isDisconnecting = $.signalR.isDisconnecting,
        transportLogic = signalR.transports._logic;

    signalR.transports.longPolling = {
        name: "longPolling",

        supportsKeepAlive: false,

        reconnectDelay: 3000,

        start: function (connection, onSuccess, onFailed) {
            /// <summary>Starts the long polling connection</summary>
            /// <param name="connection" type="signalR">The SignalR connection to start</param>
            var that = this,
                initialConnectFired = false;

            if (connection.pollXhr) {
                connection.log("Polling xhr requests already exists, aborting.");
                connection.stop();
            }

            connection.messageId = null;

            window.setTimeout(function () {
                (function poll(instance, raiseReconnect) {

                    var messageId = instance.messageId,
                        connect = (messageId === null),
                        reconnecting = !connect,
                        url = transportLogic.getUrl(instance, that.name, reconnecting, raiseReconnect),
                        reconnectTimeOut = null,
                        reconnectFired = false;

                    if (reconnecting === true && raiseReconnect === true) {
                        if (connection.state !== signalR.connectionState.reconnecting &&
                            changeState(connection,
                                        signalR.connectionState.connected,
                                        signalR.connectionState.reconnecting) === false) {
                            return;
                        }
                    }

                    connection.log("Attempting to connect to '" + url + "' using longPolling.");
                    instance.pollXhr = $.ajax({
                        url: url,
                        global: false,
                        cache: false,
                        type: "GET",
                        dataType: connection.ajaxDataType,
                        success: function (minData) {
                            var delay = 0,
                                timedOutReceived = false,
                                data;

                            if (minData) {
                                data = {
                                    // data.L is PersistentResponse.TransportData.LongPollDelay
                                    LongPollDelay: minData.L,
                                    TimedOut: typeof (minData.T) != "undefined" ? true : false,
                                    Disconnect: typeof (minData.D) != "undefined" ? true : false
                                };
                            }

                            if (initialConnectFired === false) {
                                onSuccess();
                                initialConnectFired = true;
                            }

                            if (raiseReconnect === true) {
                                // Fire the reconnect event if it hasn't been fired as yet
                                if (reconnectFired === false) {
                                    connection.log("Raising the reconnect event");

                                    if (changeState(connection,
                                                    signalR.connectionState.reconnecting,
                                                    signalR.connectionState.connected) === true) {

                                        $(instance).triggerHandler(events.onReconnect);
                                        reconnectFired = true;
                                    }
                                }
                            }

                            transportLogic.processMessages(instance, minData);
                            if (data &&
                                $.type(data.LongPollDelay) === "number") {
                                delay = data.LongPollDelay;
                            }

                            if (data && data.TimedOut) {
                                timedOutReceived = data.TimedOut;
                            }

                            if (data && data.Disconnect) {
                                return;
                            }

                            if (isDisconnecting(instance) === true) {
                                return;
                            }

                            if (delay > 0) {
                                window.setTimeout(function () {
                                    poll(instance, timedOutReceived);
                                }, delay);
                            } else {
                                poll(instance, timedOutReceived);
                            }
                        },

                        error: function (data, textStatus) {
                            if (textStatus === "abort") {
                                connection.log("Aborted xhr requst.");
                                return;
                            }

                            connection.log("An error occurred using longPolling. Status = " + textStatus + ". " + data.responseText);

                            if (reconnectTimeOut) {
                                // If the request failed then we clear the timeout so that the
                                // reconnect event doesn't get fired
                                window.clearTimeout(reconnectTimeOut);
                            }

                            $(instance).triggerHandler(events.onError, [data.responseText]);

                            window.setTimeout(function () {
                                if (isDisconnecting(instance) === false) {
                                    poll(instance, true);
                                }
                            }, connection.reconnectDelay);
                        }
                    });

                    if (raiseReconnect === true) {
                        reconnectTimeOut = window.setTimeout(function () {
                            if (reconnectFired === false) {
                                if (changeState(connection,
                                                signalR.connectionState.reconnecting,
                                                signalR.connectionState.connected) === true) {

                                    $(instance).triggerHandler(events.onReconnect);
                                    reconnectFired = true;
                                }
                            }
                        },
                        that.reconnectDelay);
                    }

                }(connection));

                // Now connected
                // There's no good way know when the long poll has actually started so
                // we assume it only takes around 150ms (max) to start the connection
                window.setTimeout(function () {
                    if (initialConnectFired === false) {
                        onSuccess();
                        initialConnectFired = true;
                    }
                }, 150);

            }, 250); // Have to delay initial poll so Chrome doesn't show loader spinner in tab
        },

        lostConnection: function (connection) {
            throw new Error("Lost Connection not handled for LongPolling");
        },

        send: function (connection, data) {
            transportLogic.ajaxSend(connection, data);
        },

        stop: function (connection) {
            /// <summary>Stops the long polling connection</summary>
            /// <param name="connection" type="signalR">The SignalR connection to stop</param>
            if (connection.pollXhr) {
                connection.pollXhr.abort();
                connection.pollXhr = null;
                delete connection.pollXhr;
            }
        },

        abort: function (connection, async) {
            transportLogic.ajaxAbort(connection, async);
        }
    };

}(window.jQuery, window));
