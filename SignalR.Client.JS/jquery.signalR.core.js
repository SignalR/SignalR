/*global window:false */
/*!
 * SignalR JavaScript Library v0.5.2
 * http://signalr.net/
 *
 * Copyright David Fowler and Damian Edwards 2012
 * Licensed under the MIT.
 * https://github.com/SignalR/SignalR/blob/master/LICENSE.md
 */

/// <reference path="Scripts/jquery-1.6.2.js" />
(function ($, window) {
    "use strict";
    
    if (typeof ($) !== "function") {
        // no jQuery!
        throw "SignalR: jQuery not found. Please ensure jQuery is referenced before the SignalR.js file.";
    }
    
    if (!window.JSON) {
        // no JSON!
        throw "SignalR: No JSON parser found. Please ensure json2.js is referenced before the SignalR.js file if you need to support clients without native JSON parsing support, e.g. IE<8.";
    }
    
    var signalR,
        _connection,

        events = {
            onStart: "onStart",
            onStarting: "onStarting",
            onSending: "onSending",
            onReceived: "onReceived",
            onError: "onError",
            onReconnect: "onReconnect",
            onStateChanged: "onStateChanged",
            onDisconnect: "onDisconnect"
        },

        log = function (msg, logging) {
            if (logging === false) {
                return;
            }
            var m;
            if (typeof (window.console) === "undefined") {
                return;
            }
            m = "[" + new Date().toTimeString() + "] SignalR: " + msg;
            if (window.console.debug) {
                window.console.debug(m);
            } else if (window.console.log) {
                window.console.log(m);
            }
        },

        isCrossDomain = function (url) {
            var link;

            url = $.trim(url);
            if (url.indexOf("http") !== 0) {
                return false;
            }

            // Create an anchor tag.
            link = window.document.createElement("a");
            link.href = url;

            return link.protocol + link.host !== window.location.protocol + window.location.host;
        },

        changeState = function (connection, expectedState, newState) {
            if (expectedState === connection.state) {
                // REVIEW: Should event fire before or after the state change actually occurs?
                $(connection).trigger(events.onStateChanged, [{ oldState: connection.state, newState: newState }]);
                connection.state = newState;
                return true;
            }

            return false;
        },

        isDisconnecting = function (connection) {
            return connection.state === signalR.connectionState.disconnected;
        };

    signalR = function (url, qs, logging) {
        /// <summary>Creates a new SignalR connection for the given url</summary>
        /// <param name="url" type="String">The URL of the long polling endpoint</param>
        /// <param name="qs" type="Object">
        ///     [Optional] Custom querystring parameters to add to the connection URL.
        ///     If an object, every non-function member will be added to the querystring.
        ///     If a string, it's added to the QS as specified.
        /// </param>
        /// <param name="logging" type="Boolean">
        ///     [Optional] A flag indicating whether connection logging is enabled to the browser
        ///     console/log. Defaults to false.
        /// </param>

        return new signalR.fn.init(url, qs, logging);
    };
    
    signalR.events = events;

    signalR.changeState = changeState;

    signalR.isDisconnecting = isDisconnecting;

    signalR.connectionState = {
        connecting: 0,
        connected: 1,
        reconnecting: 2,
        disconnected: 4
    };
    
    signalR.hub = {
        start: function () {
            // This will get replaced with the real hub connection start method when hubs is referenced correctly
            throw "SignalR: Error loading hubs. Ensure your hubs reference is correct, e.g. <script src='/signalr/hubs'></script>.";
        }
    };

    signalR.fn = signalR.prototype = {
        init: function (url, qs, logging) {
            this.url = url;
            this.qs = qs;
            if (typeof (logging) === "boolean") {
                this.logging = logging;
            }
        },

        ajaxDataType: "json",

        logging: false,

        state: signalR.connectionState.disconnected,

        reconnectDelay: 2000,

        start: function (options, callback) {
            /// <summary>Starts the connection</summary>
            /// <param name="options" type="Object">Options map</param>
            /// <param name="callback" type="Function">A callback function to execute when the connection has started</param>
            var connection = this,
                config = {
                    transport: "auto",
                    jsonp: false
                },
                initialize,
                deferred = $.Deferred(),
                parser = window.document.createElement("a");

            if (changeState(connection,
                            signalR.connectionState.disconnected,
                            signalR.connectionState.connecting) === false) {
                // Already started, just return
                deferred.resolve(connection);
                return deferred.promise();
            }

            if ($.type(options) === "function") {
                // Support calling with single callback parameter
                callback = options;
            } else if ($.type(options) === "object") {
                $.extend(config, options);
                if ($.type(config.callback) === "function") {
                    callback = config.callback;
                }
            }

            // Resolve the full url
            parser.href = connection.url;
            if (!parser.protocol || parser.protocol === ":") {
                connection.protocol = window.document.location.protocol;
                connection.host = window.document.location.host;
                connection.baseUrl = connection.protocol + "//" + connection.host;
            }
            else {
                connection.protocol = parser.protocol;
                connection.host = parser.host;
                connection.baseUrl = parser.protocol + "//" + parser.host;
            }

            // Set the websocket protocol
            connection.wsProtocol = connection.protocol === "https:" ? "wss://" : "ws://";

            if (isCrossDomain(connection.url)) {
                connection.log("Auto detected cross domain url.");

                if (config.transport === "auto") {
                    // If you didn't say you wanted to use jsonp, determine if it's your only choice
                    // i.e. if your browser doesn't supports CORS
                    if (!config.jsonp) {
                        config.jsonp = !$.support.cors;

                        if (config.jsonp) {
                            connection.log("Using jsonp because this browser doesn't support CORS");
                        }
                    }

                    // If we're using jsonp thn just change to longpolling
                    if (config.jsonp === true) {
                        config.transport = "longPolling";
                    }
                    else {
                        // Otherwise try webSockets and longPolling since SSE doesn't support CORS
                        // TODO: Support XDM with foreverFrame
                        config.transport = ["webSockets", "longPolling"];
                    }
                }
            }

            connection.ajaxDataType = config.jsonp ? "jsonp" : "json";

            $(connection).bind(events.onStart, function (e, data) {
                if ($.type(callback) === "function") {
                    callback.call(connection);
                }
                deferred.resolve(connection);
            });

            initialize = function (transports, index) {
                index = index || 0;
                if (index >= transports.length) {
                    if (!connection.transport) {
                        // No transport initialized successfully
                        deferred.reject("SignalR: No transport could be initialized successfully. Try specifying a different transport or none at all for auto initialization.");
                    }
                    return;
                }

                var transportName = transports[index],
                    transport = $.type(transportName) === "object" ? transportName : signalR.transports[transportName];

                if (transportName.indexOf("_") === 0) {
                    // Private member
                    initialize(transports, index + 1);
                    return;
                }

                transport.start(connection, function () { // success
                    connection.transport = transport;

                    changeState(connection,
                                signalR.connectionState.connecting,
                                signalR.connectionState.connected);

                    $(connection).trigger(events.onStart);

                    $(window).unload(function () { // failure
                        connection.stop(false /* async */);
                    });

                }, function () {
                    initialize(transports, index + 1);
                });
            };

            window.setTimeout(function () {
                var url = connection.url + "/negotiate";
                connection.log("Negotiating with '" + url + "'.");
                $.ajax({
                    url: url,
                    global: false,
                    cache: false,
                    type: "GET",
                    data: {},
                    dataType: connection.ajaxDataType,
                    error: function (error) {
                        $(connection).trigger(events.onError, [error.responseText]);
                        deferred.reject("SignalR: Error during negotiation request: " + error.responseText);
                        // Stop the connection if negotiate failed
                        connection.stop();
                    },
                    success: function (res) {
                        connection.appRelativeUrl = res.Url;
                        connection.id = res.ConnectionId;
                        connection.webSocketServerUrl = res.WebSocketServerUrl;

                        if (!res.ProtocolVersion || res.ProtocolVersion !== "1.0") {
                            $(connection).trigger(events.onError, "SignalR: Incompatible protocol version.");
                            deferred.reject("SignalR: Incompatible protocol version.");
                            return;
                        }

                        $(connection).trigger(events.onStarting);

                        var transports = [],
                            supportedTransports = [];

                        $.each(signalR.transports, function (key) {
                            if (key === "webSockets" && !res.TryWebSockets) {
                                // Server said don't even try WebSockets, but keep processing the loop
                                return true;
                            }
                            supportedTransports.push(key);
                        });

                        if ($.isArray(config.transport)) {
                            // ordered list provided
                            $.each(config.transport, function () {
                                var transport = this;
                                if ($.type(transport) === "object" || ($.type(transport) === "string" && $.inArray("" + transport, supportedTransports) >= 0)) {
                                    transports.push($.type(transport) === "string" ? "" + transport : transport);
                                }
                            });
                        } else if ($.type(config.transport) === "object" ||
                                       $.inArray(config.transport, supportedTransports) >= 0) {
                                // specific transport provided, as object or a named transport, e.g. "longPolling"
                            transports.push(config.transport);
                        } else { // default "auto"
                            transports = supportedTransports;
                        }
                        initialize(transports);
                    }
                });
            }, 0);

            return deferred.promise();
        },

        starting: function (callback) {
            /// <summary>Adds a callback that will be invoked before the connection is started</summary>
            /// <param name="callback" type="Function">A callback function to execute when the connection is starting</param>
            /// <returns type="signalR" />
            var connection = this,
                $connection = $(connection);

            $connection.bind(events.onStarting, function (e, data) {
                callback.call(connection);
                // Unbind immediately, we don't want to call this callback again
                $connection.unbind(events.onStarting);
            });

            return connection;
        },

        send: function (data) {
            /// <summary>Sends data over the connection</summary>
            /// <param name="data" type="String">The data to send over the connection</param>
            /// <returns type="signalR" />
            var connection = this;

            if (connection.state === signalR.connectionState.disconnected) {
                // Connection hasn't been started yet
                throw "SignalR: Connection must be started before data can be sent. Call .start() before .send()";
            }

            if (connection.state === signalR.connectionState.connecting) {
                // Connection hasn't been started yet
                throw "SignalR: Connection has not been fully initialized. Use .start().done() or .start().fail() to run logic after the connection has started.";
            }

            connection.transport.send(connection, data);
            // REVIEW: Should we return deferred here?
            return connection;
        },

        sending: function (callback) {
            /// <summary>Adds a callback that will be invoked before anything is sent over the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute before each time data is sent on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onSending, function (e, data) {
                callback.call(connection);
            });
            return connection;
        },

        received: function (callback) {
            /// <summary>Adds a callback that will be invoked after anything is received over the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute when any data is received on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onReceived, function (e, data) {
                callback.call(connection, data);
            });
            return connection;
        },

        stateChanged: function (callback) {
            /// <summary>Adds a callback that will be invoked when the connection state changes</summary>
            /// <param name="callback" type="Function">A callback function to execute when the connection state changes</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onStateChanged, function (e, data) {
                callback.call(connection, data);
            });
            return connection;
        },

        error: function (callback) {
            /// <summary>Adds a callback that will be invoked after an error occurs with the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute when an error occurs on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onError, function (e, data) {
                callback.call(connection, data);
            });
            return connection;
        },

        disconnected: function (callback) {
            /// <summary>Adds a callback that will be invoked when the client disconnects</summary>
            /// <param name="callback" type="Function">A callback function to execute when the connection is broken</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onDisconnect, function (e, data) {
                callback.call(connection);
            });
            return connection;
        },

        reconnected: function (callback) {
            /// <summary>Adds a callback that will be invoked when the underlying transport reconnects</summary>
            /// <param name="callback" type="Function">A callback function to execute when the connection is restored</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind(events.onReconnect, function (e, data) {
                callback.call(connection);
            });
            return connection;
        },

        stop: function (async) {
            /// <summary>Stops listening</summary>
            /// <returns type="signalR" />
            var connection = this;

            if (connection.state === signalR.connectionState.disconnected) {
                return;
            }

            try {
                if (connection.transport) {
                    connection.transport.abort(connection, async);
                    connection.transport.stop(connection);
                    connection.transport = null;
                }

                // Trigger the disconnect event
                $(connection).trigger(events.onDisconnect);

                delete connection.messageId;
                delete connection.groups;
            }
            finally {
                changeState(connection, connection.state, signalR.connectionState.disconnected);
            }

            return connection;
        },

        log: function (msg) {
            log(msg, this.logging);
        }
    };

    signalR.fn.init.prototype = signalR.fn;

    signalR.noConflict = function () {
        /// <summary>Reinstates the original value of $.connection and returns the signalR object for manual assignment</summary>
        /// <returns type="signalR" />
        if ($.connection === signalR) {
            $.connection = _connection;
        }
        return signalR;
    };

    if ($.connection) {
        _connection = $.connection;
    }

    $.connection = $.signalR = signalR;

}(window.jQuery, window));