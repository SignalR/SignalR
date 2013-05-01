﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
                fireConnect = function () {
                    onSuccess();
                    connection.log("Longpolling connected");
                },
                reconnectErrors = 0,
                reconnectTimeoutId = null,
                fireReconnected = function (instance) {
                    window.clearTimeout(reconnectTimeoutId);
                    reconnectTimeoutId = null;

                    if (changeState(connection,
                                    signalR.connectionState.reconnecting,
                                    signalR.connectionState.connected) === true) {
                        // Successfully reconnected!
                        connection.log("Raising the reconnect event");
                        $(instance).triggerHandler(events.onReconnect);
                    }
                },
                // 1 hour
                maxFireReconnectedTimeout = 3600000;

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
                        polling = !raiseReconnect,
                        url = transportLogic.getUrl(instance, that.name, reconnecting, polling);

                    // If we've disconnected during the time we've tried to re-instantiate the poll then stop.
                    if (isDisconnecting(instance) === true) {
                        return;
                    }

                    connection.log("Attempting to connect to '" + url + "' using longPolling.");
                    instance.pollXhr = $.ajax({
                        url: url,
                        global: false,
                        cache: false,
                        type: "GET",
                        dataType: connection.ajaxDataType,
                        contentType: connection.contentType,
                        success: function (minData) {
                            var delay = 0,
                                data;

                            // Reset our reconnect errors so if we transition into a reconnecting state again we trigger
                            // reconnected quickly
                            reconnectErrors = 0;

                            // If there's currently a timeout to trigger reconnect, fire it now before processing messages
                            if (reconnectTimeoutId !== null) {
                                fireReconnected();
                            }

                            if (minData) {
                                data = transportLogic.maximizePersistentResponse(minData);
                            }

                            // If we did not initialize (we could already be initialized)
                            if (!transportLogic.tryInitialize(data, fireConnect)) {
                                // Try to buffer only if we're still trying to connect to the server.
                                // Protects against server race where data can come to the client faster
                                // than the initialize message.
                                if (transportLogic.tryPreConnectBuffer(instance, minData)) {
                                    // We've successfully buffered a message so re-start poll to wait for 
                                    // the initialize message
                                    poll(instance, false);
                                    return;
                                }
                            }

                            transportLogic.processMessages(instance, minData);

                            if (data &&
                                $.type(data.LongPollDelay) === "number") {
                                delay = data.LongPollDelay;
                            }

                            if (data && data.Disconnect) {
                                return;
                            }

                            if (isDisconnecting(instance) === true) {
                                return;
                            }

                            // We never want to pass a raiseReconnect flag after a successful poll.  This is handled via the error function
                            if (delay > 0) {
                                window.setTimeout(function () {
                                    poll(instance, false);
                                }, delay);
                            } else {
                                poll(instance, false);
                            }
                        },

                        error: function (data, textStatus) {
                            // Stop trying to trigger reconnect, connection is in an error state
                            // If we're not in the reconnect state this will noop
                            window.clearTimeout(reconnectTimeoutId);
                            reconnectTimeoutId = null;

                            if (textStatus === "abort") {
                                connection.log("Aborted xhr requst.");
                                return;
                            }

                            // Increment our reconnect errors, we assume all errors to be reconnect errors
                            // In the case that it's our first error this will cause Reconnect to be fired
                            // after 1 second due to reconnectErrors being = 1.
                            reconnectErrors++;

                            if (connection.state !== signalR.connectionState.reconnecting) {
                                connection.log("An error occurred using longPolling. Status = " + textStatus + ". " + data.responseText);
                                $(instance).triggerHandler(events.onError, [data.responseText]);
                            }

                            // Transition into the reconnecting state
                            transportLogic.ensureReconnectingState(instance);

                            // Call poll with the raiseReconnect flag as true after the reconnect delay
                            window.setTimeout(function () {
                                poll(instance, true);
                            }, that.reconnectDelay);
                        }
                    });


                    // This will only ever pass after an error has occured via the poll ajax procedure.
                    if (reconnecting && raiseReconnect === true) {
                        // We wait to reconnect depending on how many times we've failed to reconnect.
                        // This is essentially a heuristic that will exponentially increase in wait time before
                        // triggering reconnected.  This depends on the "error" handler of Poll to cancel this 
                        // timeout if it triggers before the Reconnected event fires.
                        // The Math.min at the end is to ensure that the reconnect timeout does not overflow.
                        reconnectTimeoutId = window.setTimeout(function () { fireReconnected(instance); }, Math.min(1000 * (Math.pow(2, reconnectErrors) - 1), maxFireReconnectedTimeout));
                    }
                }(connection, false));
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
