// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.core.js" />

(function ($, window, undefined) {
    "use strict";

    var signalR = $.signalR,
        events = $.signalR.events,
        changeState = $.signalR.changeState,
        transportLogic;

    signalR.transports = {};

    function beat(connection) {
        if (connection._.keepAliveData.monitoring) {
            checkIfAlive(connection);
        }

        // Ensure that we successfully marked active before continuing the heartbeat.
        if (transportLogic.markActive(connection)) {
            connection._.beatHandle = window.setTimeout(function () {
                beat(connection);
            }, connection._.beatInterval);
        }
    }

    function checkIfAlive(connection) {
        var keepAliveData = connection._.keepAliveData,
            timeElapsed;

        // Only check if we're connected
        if (connection.state === signalR.connectionState.connected) {
            timeElapsed = new Date().getTime() - connection._.lastMessageAt;

            // Check if the keep alive has completely timed out
            if (timeElapsed >= keepAliveData.timeout) {
                connection.log("Keep alive timed out.  Notifying transport that connection has been lost.");

                // Notify transport that the connection has been lost
                connection.transport.lostConnection(connection);
            }
            else if (timeElapsed >= keepAliveData.timeoutWarning) {
                // This is to assure that the user only gets a single warning
                if (!keepAliveData.userNotified) {
                    connection.log("Keep alive has been missed, connection may be dead/slow.");
                    $(connection).triggerHandler(events.onConnectionSlow);
                    keepAliveData.userNotified = true;
                }
            }
            else {
                keepAliveData.userNotified = false;
            }
        }
    }

    function isConnectedOrReconnecting(connection) {
        return connection.state === signalR.connectionState.connected ||
               connection.state === signalR.connectionState.reconnecting;
    }

    function addConnectionData(url, connectionData) {
        var appender = url.indexOf("?") !== -1 ? "&" : "?";

        if (connectionData) {
            url += appender + "connectionData=" + window.encodeURIComponent(connectionData);
        }

        return url;
    }

    transportLogic = signalR.transports._logic = {
        pingServer: function (connection) {
            /// <summary>Pings the server</summary>
            /// <param name="connection" type="signalr">Connection associated with the server ping</param>
            /// <returns type="signalR" />
            var url, deferral = $.Deferred();

            url = connection.url + "/ping";
            url = transportLogic.prepareQueryString(connection, url);

            $.ajax(
                $.extend({}, $.signalR.ajaxDefaults, {
                    xhrFields: { withCredentials: connection.withCredentials },
                    url: url,
                    type: "GET",
                    contentType: connection.contentType,
                    data: {},
                    dataType: connection.ajaxDataType,
                    success: function (result) {
                        var data;
                        
                        try {
                            data = connection._parseResponse(result);
                        }
                        catch (error) {
                            deferral.reject("Failed to parse ping server response, stopping the connection: " + result);
                            connection.stop();
                            return;
                        }

                        if (data.Response === "pong") {
                            deferral.resolve();
                        }
                        else {
                            deferral.reject("SignalR: Invalid ping response when pinging server: " + data.Response);
                        }
                    },
                    error: function (error) {
                        if (error.status === 401 || error.status === 403) {
                            deferral.reject("Failed to ping server. Server responded with a " + error.status + " status code, stopping the connection.");
                            connection.stop();
                        }
                        else {
                            deferral.reject("SignalR: Error pinging server: " + (error.responseText || error.statusText));
                        }
                    }
                }));

            return deferral.promise();
        },

        prepareQueryString: function (connection, url) {
            url = transportLogic.addQs(url, connection);

            return addConnectionData(url, connection.data);
        },

        addQs: function (url, connection) {
            var appender = url.indexOf("?") !== -1 ? "&" : "?",
                firstChar;

            if (!connection.qs) {
                return url;
            }

            if (typeof (connection.qs) === "object") {
                return url + appender + $.param(connection.qs);
            }

            if (typeof (connection.qs) === "string") {
                firstChar = connection.qs.charAt(0);

                if (firstChar === "?" || firstChar === "&") {
                    appender = "";
                }

                return url + appender + connection.qs;
            }

            throw new Error("Connections query string property must be either a string or object.");
        },

        getUrl: function (connection, transport, reconnecting, poll) {
            /// <summary>Gets the url for making a GET based connect request</summary>
            var baseUrl = transport === "webSockets" ? "" : connection.baseUrl,
                url = baseUrl + connection.appRelativeUrl,
                qs = "transport=" + transport + "&connectionToken=" + window.encodeURIComponent(connection.token);

            if (connection.groupsToken) {
                qs += "&groupsToken=" + window.encodeURIComponent(connection.groupsToken);
            }

            if (!reconnecting) {
                url += "/connect";
            } else {
                if (poll) {
                    // longPolling transport specific
                    url += "/poll";
                } else {
                    url += "/reconnect";
                }

                if (connection.messageId) {
                    qs += "&messageId=" + window.encodeURIComponent(connection.messageId);
                }
            }
            url += "?" + qs;
            url = transportLogic.prepareQueryString(connection, url);
            url += "&tid=" + Math.floor(Math.random() * 11);
            return url;
        },

        maximizePersistentResponse: function (minPersistentResponse) {
            return {
                MessageId: minPersistentResponse.C,
                Messages: minPersistentResponse.M,
                Disconnect: typeof (minPersistentResponse.D) !== "undefined" ? true : false,
                ShouldReconnect: typeof (minPersistentResponse.T) !== "undefined" ? true : false,
                LongPollDelay: minPersistentResponse.L,
                GroupsToken: minPersistentResponse.G
            };
        },

        updateGroups: function (connection, groupsToken) {
            if (groupsToken) {
                connection.groupsToken = groupsToken;
            }
        },

        ajaxSend: function (connection, data) {
            var url = connection.url + "/send" + "?transport=" + connection.transport.name + "&connectionToken=" + window.encodeURIComponent(connection.token),
                onFail = function (error, connection) {
                    $(connection).triggerHandler(events.onError, [error, data]);
                };

            url = transportLogic.prepareQueryString(connection, url);
            
            return $.ajax(
                $.extend({}, $.signalR.ajaxDefaults, {
                    xhrFields: { withCredentials: connection.withCredentials },
                    url: url,
                    type: connection.ajaxDataType === "jsonp" ? "GET" : "POST",
                    contentType: signalR._.defaultContentType,
                    dataType: connection.ajaxDataType,
                    data: {
                        data: data
                    },
                    success: function (result) {
                        var res;

                        if (result) {
                            try {
                                res = connection._parseResponse(result);
                            }
                            catch (error) {
                                onFail(error, connection);
                                connection.stop();
                                return;
                            }

                            $(connection).triggerHandler(events.onReceived, [res]);
                        }
                    },
                    error: function (error, textStatus) {
                        if (textStatus === "abort" || textStatus === "parsererror") {
                            // The parsererror happens for sends that don't return any data, and hence
                            // don't write the jsonp callback to the response. This is harder to fix on the server
                            // so just hack around it on the client for now.
                            return;
                        }

                        onFail(error, connection);
                    }
                }));
        },

        ajaxAbort: function (connection, async) {
            if (typeof (connection.transport) === "undefined") {
                return;
            }

            // Async by default unless explicitly overidden
            async = typeof async === "undefined" ? true : async;

            var url = connection.url + "/abort" + "?transport=" + connection.transport.name + "&connectionToken=" + window.encodeURIComponent(connection.token);
            url = transportLogic.prepareQueryString(connection, url);

            $.ajax(
                $.extend({}, $.signalR.ajaxDefaults, {
                    xhrFields: { withCredentials: connection.withCredentials },
                    url: url,
                    async: async,
                    timeout: 1000,
                    type: "POST",
                    contentType: connection.contentType,
                    dataType: connection.ajaxDataType,
                    data: {}
                }));

            connection.log("Fired ajax abort async = " + async + ".");
        },

        processMessages: function (connection, minData) {
            var data;
            // Transport can be null if we've just closed the connection
            if (connection.transport) {
                var $connection = $(connection);

                // Update the last message time stamp
                transportLogic.markLastMessage(connection);

                if (!minData) {
                    return;
                }

                data = this.maximizePersistentResponse(minData);

                if (data.Disconnect) {
                    connection.log("Disconnect command received from server.");

                    // Disconnected by the server
                    connection.stop(false, false);
                    return;
                }

                this.updateGroups(connection, data.GroupsToken);

                if (data.Messages) {
                    $.each(data.Messages, function (index, message) {
                        $connection.triggerHandler(events.onReceived, [message]);
                    });
                }

                if (data.MessageId) {
                    connection.messageId = data.MessageId;
                }
            }
        },

        monitorKeepAlive: function (connection) {
            var keepAliveData = connection._.keepAliveData;

            // If we haven't initiated the keep alive timeouts then we need to
            if (!keepAliveData.monitoring) {
                keepAliveData.monitoring = true;

                // Initialize the keep alive time stamp ping
                transportLogic.markLastMessage(connection);

                // Save the function so we can unbind it on stop
                connection._.keepAliveData.reconnectKeepAliveUpdate = function () {
                    // Mark a new message so that keep alive doesn't time out connections
                    transportLogic.markLastMessage(connection);
                };

                // Update Keep alive on reconnect
                $(connection).bind(events.onReconnect, connection._.keepAliveData.reconnectKeepAliveUpdate);

                connection.log("Now monitoring keep alive with a warning timeout of " + keepAliveData.timeoutWarning + " and a connection lost timeout of " + keepAliveData.timeout + ".");
            }
            else {
                connection.log("Tried to monitor keep alive but it's already being monitored.");
            }
        },

        stopMonitoringKeepAlive: function (connection) {
            var keepAliveData = connection._.keepAliveData;

            // Only attempt to stop the keep alive monitoring if its being monitored
            if (keepAliveData.monitoring) {
                // Stop monitoring
                keepAliveData.monitoring = false;

                // Remove the updateKeepAlive function from the reconnect event
                $(connection).unbind(events.onReconnect, connection._.keepAliveData.reconnectKeepAliveUpdate);

                // Clear all the keep alive data
                connection._.keepAliveData = {};
                connection.log("Stopping the monitoring of the keep alive.");
            }
        },

        startHeartbeat: function (connection) {
            beat(connection);
        },

        markLastMessage: function (connection) {
            connection._.lastMessageAt = new Date().getTime();
        },

        markActive: function (connection) {
            if (transportLogic.verifyLastActive(connection)) {
                connection._.lastActiveAt = new Date().getTime();
                return true;
            }

            return false;
        },

        ensureReconnectingState: function (connection) {
            if (changeState(connection,
                        signalR.connectionState.connected,
                        signalR.connectionState.reconnecting) === true) {
                $(connection).triggerHandler(events.onReconnecting);
            }
            return connection.state === signalR.connectionState.reconnecting;
        },

        clearReconnectTimeout: function (connection) {
            if (connection && connection._.reconnectTimeout) {
                window.clearTimeout(connection._.reconnectTimeout);
                delete connection._.reconnectTimeout;
            }
        },

        verifyLastActive: function (connection) {
            if (new Date().getTime() - connection._.lastActiveAt >= connection.reconnectWindow) {
                connection.log("There has not been an active server connection for an extended periord of time. Stopping connection.");
                connection.stop();
                return false;
            }

            return true;
        },

        reconnect: function (connection, transportName) {
            var transport = signalR.transports[transportName],
                that = this;

            // We should only set a reconnectTimeout if we are currently connected
            // and a reconnectTimeout isn't already set.
            if (isConnectedOrReconnecting(connection) && !connection._.reconnectTimeout) {
                // Need to verify before the setTimeout occurs because an application sleep could occur during the setTimeout duration.
                if (!transportLogic.verifyLastActive(connection)) {
                    return;
                }

                connection._.reconnectTimeout = window.setTimeout(function () {
                    if (!transportLogic.verifyLastActive(connection)) {
                        return;
                    }

                    transport.stop(connection);

                    if (that.ensureReconnectingState(connection)) {
                        connection.log(transportName + " reconnecting.");
                        transport.start(connection);
                    }
                }, connection.reconnectDelay);
            }
        },

        handleParseFailure: function (connection, result, errorMessage, onFailed) {
            // If we're in the initialization phase trigger onFailed, otherwise stop the connection.
            if (connection.state === signalR.connectionState.connecting) {
                connection.log("Failed to parse server response while attempting to connect.");
                onFailed();
            }
            else {
                $(connection).triggerHandler(events.onError, ["SignalR: failed at parsing response: " + result + ".  With error: " + errorMessage]);
                connection.stop();
            }
        },

        foreverFrame: {
            count: 0,
            connections: {}
        }
    };

}(window.jQuery, window));
