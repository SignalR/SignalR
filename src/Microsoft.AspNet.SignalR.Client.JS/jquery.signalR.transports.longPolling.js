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

        init: function (connection, onComplete) {
            /// <summary>Pings the server to ensure availability</summary>
            /// <param name="connection" type="signalr">Connection associated with the server ping</param>
            /// <param name="onComplete" type="Function">Callback to call once initialization has completed</param>

            var that = this,
                pingLoop,
                pingFail = function (reason) {
                    if (isDisconnecting(connection) === false) {
                        connection.log("SignalR: Server ping failed because '" + reason + "', re-trying ping.");
                        window.setTimeout(pingLoop, that.reconnectDelay);
                    }
                };
            
            pingLoop = function () {
                transportLogic.pingServer(connection, that.name).done(onComplete).fail(pingFail);
            };

            pingLoop();
        },        

        start: function (connection, onSuccess, onFailed) {
            /// <summary>Starts the long polling connection</summary>
            /// <param name="connection" type="signalR">The SignalR connection to start</param>
            var that = this;

            if (connection.pollXhr) {
                connection.log("Polling xhr requests already exists, aborting.");
                connection.stop();
            }

            that.init(connection, function () {
                connection.messageId = null;

                window.setTimeout(function () {
                    (function poll(instance, raiseReconnect) {
                        var messageId = instance.messageId,
                            connect = (messageId === null),
                            reconnecting = !connect,
                            url = transportLogic.getUrl(instance, that.name, reconnecting, raiseReconnect),
                            reconnectTimeOut = null,
                            reconnectFired = false,
                            triggerReconnected = function () {
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
                            };

                        if (reconnecting === true && raiseReconnect === true &&
                            !transportLogic.ensureReconnectingState(connection)) {
                            return;
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
                                    data = transportLogic.maximizePersistentResponse(minData);
                                }

                                if (raiseReconnect === true) {
                                    triggerReconnected();
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

                                if (connection.state !== signalR.connectionState.reconnecting) {
                                    connection.log("An error occurred using longPolling. Status = " + textStatus + ". " + data.responseText);
                                    $(instance).triggerHandler(events.onError, [data.responseText]);
                                }

                                that.init(instance, function () {
                                    poll(instance, true);
                                });
                            }
                        });

                        if (raiseReconnect === true) {
                            reconnectTimeOut = window.setTimeout(triggerReconnected, that.reconnectDelay);
                        }
                    }(connection));

                    // Trigger the onSuccess() method because we've now instantiated a connection
                    onSuccess();
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
