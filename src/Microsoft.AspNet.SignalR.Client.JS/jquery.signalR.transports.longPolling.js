// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.transports.common.js" />

(function ($, window, undefined) {
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

        init: function (connection, onComplete) {
            /// <summary>Pings the server to ensure availability</summary>
            /// <param name="connection" type="signalr">Connection associated with the server ping</param>
            /// <param name="onComplete" type="Function">Callback to call once initialization has completed</param>

            var that = this,
                pingLoop,
                // pingFail is used to loop the re-ping behavior.  When we fail we want to re-try.
                pingFail = function (reason) {
                    if (isDisconnecting(connection) === false) {
                        connection.log("Server ping failed because '" + reason + "', re-trying ping.");
                        window.setTimeout(pingLoop, that.reconnectDelay);
                    }
                };

            connection.log("Initializing long polling connection with server.");
            pingLoop = function () {
                // Ping the server, on successful ping call the onComplete method, otherwise if we fail call the pingFail
                transportLogic.pingServer(connection).done(onComplete).fail(pingFail);
            };

            pingLoop();
        },

        start: function (connection, onSuccess, onFailed) {
            /// <summary>Starts the long polling connection</summary>
            /// <param name="connection" type="signalR">The SignalR connection to start</param>
            var that = this,
                fireConnect = function () {
                    tryFailConnect = fireConnect = $.noop;

                    connection.log("Longpolling connected.");
                    onSuccess();

                    // Reset onFailed to null because it shouldn't be called again
                    onFailed = null;
                },
                tryFailConnect = function () {
                    if (onFailed) {
                        onFailed();
                        onFailed = null;
                        connection.log("LongPolling failed to connect.");
                        return true;
                    }

                    return false;
                },
                privateData = connection._,
                reconnectErrors = 0,
                fireReconnected = function (instance) {
                    window.clearTimeout(privateData.reconnectTimeoutId);
                    privateData.reconnectTimeoutId = null;

                    if (changeState(connection,
                                    signalR.connectionState.reconnecting,
                                    signalR.connectionState.connected) === true) {
                        // Successfully reconnected!
                        connection.log("Raising the reconnect event.");
                        $(instance).triggerHandler(events.onReconnect);
                    }
                },
                // 1 hour
                maxFireReconnectedTimeout = 3600000;

            if (connection.pollXhr) {
                connection.log("Polling xhr requests already exists, aborting.");
                connection.stop();
            }

            privateData.reconnectTimeoutId = null;
            privateData.pollTimeoutId = null;

            // We start with an initialization procedure which pings the server to verify that it is there.
            // On scucessful initialization we'll then proceed with starting the transport.
            that.init(connection, function () {
                connection.messageId = null;

                privateData.pollTimeoutId = window.setTimeout(function () {
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

                        connection.log("Opening long polling request to '" + url + "'.");
                        instance.pollXhr = $.ajax(
                            $.extend({}, $.signalR.ajaxDefaults, {
                                xhrFields: { withCredentials: connection.withCredentials },
                                url: url,
                                type: "GET",
                                dataType: connection.ajaxDataType,
                                contentType: connection.contentType,
                                success: function (result) {
                                    var delay = 0,
                                        minData,
                                        data,
                                        shouldReconnect;

                                    connection.log("Long poll complete.");

                                    // Reset our reconnect errors so if we transition into a reconnecting state again we trigger
                                    // reconnected quickly
                                    reconnectErrors = 0;

                                    try {
                                        minData = connection._parseResponse(result);
                                    }
                                    catch (error) {
                                        transportLogic.handleParseFailure(instance, result, error.message, tryFailConnect);
                                        return;
                                    }

                                    // If there's currently a timeout to trigger reconnect, fire it now before processing messages
                                    if (privateData.reconnectTimeoutId !== null) {
                                        fireReconnected();
                                    }

                                    fireConnect();

                                    if (minData) {
                                        data = transportLogic.maximizePersistentResponse(minData);
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

                                    shouldReconnect = data && data.ShouldReconnect;
                                    if (shouldReconnect) {
                                        // Transition into the reconnecting state
                                        transportLogic.ensureReconnectingState(instance);
                                    }

                                    // We never want to pass a raiseReconnect flag after a successful poll.  This is handled via the error function
                                    if (delay > 0) {
                                        privateData.pollTimeoutId = window.setTimeout(function () {
                                            poll(instance, shouldReconnect);
                                        }, delay);
                                    } else {
                                        poll(instance, shouldReconnect);
                                    }
                                },

                                error: function (data, textStatus) {
                                    // Stop trying to trigger reconnect, connection is in an error state
                                    // If we're not in the reconnect state this will noop
                                    window.clearTimeout(privateData.reconnectTimeoutId);
                                    privateData.reconnectTimeoutId = null;

                                    if (textStatus === "abort") {
                                        connection.log("Aborted xhr requst.");
                                        return;
                                    }

                                    if (!tryFailConnect()) {
                                        // Increment our reconnect errors, we assume all errors to be reconnect errors
                                        // In the case that it's our first error this will cause Reconnect to be fired
                                        // after 1 second due to reconnectErrors being = 1.
                                        reconnectErrors++;

                                        if (connection.state !== signalR.connectionState.reconnecting) {
                                            connection.log("An error occurred using longPolling. Status = " + textStatus + ".  Response = " + data.responseText + ".");
                                            $(instance).triggerHandler(events.onError, [data.responseText]);
                                        }

                                        // We check the state here to verify that we're not in an invalid state prior to verifying Reconnect.
                                        // If we're not in connected or reconnecting then the next ensureReconnectingState check will fail and will return.
                                        // Therefore we don't want to change that failure code path.
                                        if ((connection.state === signalR.connectionState.connected ||
                                            connection.state === signalR.connectionState.reconnecting) &&
                                            !transportLogic.verifyReconnect(connection)) {
                                            return;
                                        }

                                        // Transition into the reconnecting state
                                        // If this fails then that means that the user transitioned the connection into the disconnected or connecting state within the above error handler trigger.
                                        if (!transportLogic.ensureReconnectingState(instance)) {
                                            return;
                                        }

                                        privateData.pollTimeoutId = window.setTimeout(function () {
                                            // If we've errored out we need to verify that the server is still there, so re-start initialization process
                                            // This will ping the server until it successfully gets a response.
                                            that.init(instance, function () {
                                                // Call poll with the raiseReconnect flag as true
                                                poll(instance, true);
                                            });
                                        }, that.reconnectDelay);
                                    }
                                }
                            }));


                        // This will only ever pass after an error has occured via the poll ajax procedure.
                        if (reconnecting && raiseReconnect === true) {
                            // We wait to reconnect depending on how many times we've failed to reconnect.
                            // This is essentially a heuristic that will exponentially increase in wait time before
                            // triggering reconnected.  This depends on the "error" handler of Poll to cancel this 
                            // timeout if it triggers before the Reconnected event fires.
                            // The Math.min at the end is to ensure that the reconnect timeout does not overflow.
                            privateData.reconnectTimeoutId = window.setTimeout(function () { fireReconnected(instance); }, Math.min(1000 * (Math.pow(2, reconnectErrors) - 1), maxFireReconnectedTimeout));
                        }
                    }(connection));

                    // Set an arbitrary timeout to trigger onSuccess, this will alot for enough time on the server to wire up the connection.
                    // Will be fixed by #1189 and this code can be modified to not be a timeout
                    window.setTimeout(function () {
                        // Trigger the onSuccess() method because we've now instantiated a connection
                        fireConnect();
                    }, 250);
                }, 250); // Have to delay initial poll so Chrome doesn't show loader spinner in tab
            });
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
            window.clearTimeout(connection._.pollTimeoutId);
            window.clearTimeout(connection._.reconnectTimeoutId);

            delete connection._.pollTimeoutId;
            delete connection._.reconnectTimeoutId;

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
