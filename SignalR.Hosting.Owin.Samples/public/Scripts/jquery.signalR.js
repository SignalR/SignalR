/// <reference path="jquery-1.6.2.js" />
(function ($, window) {
    /// <param name="$" type="jQuery" />
    "use strict";

    if (typeof ($) !== "function") {
        // no jQuery!
        throw "SignalR: jQuery not found. Please ensure jQuery is referenced before the SignalR.js file.";
    }

    if (!window.JSON) {
        // no JSON!
        throw "SignalR: No JSON parser found. Please ensure json2.js is referenced before the SignalR.js file if you need to support clients without native JSON parsing support, e.g. IE<8.";
    }

    var signalR, _connection,
        log = (typeof (console) !== "undefined" && console && console.debug)
            ? function (msg) { console.debug(msg); }
            : $.noop;

    signalR = function (url) {
        /// <summary>Creates a new SignalR connection for the given url</summary>
        /// <param name="url" type="String">The URL of the long polling endpoint</param>
        /// <returns type="signalR" />

        return new signalR.fn.init(url);
    };

    signalR.fn = signalR.prototype = {
        init: function (url) {
            this.url = url;
        },

        start: function (options, callback) {
            /// <summary>Starts the connection</summary>
            /// <param name="options" type="Object">Options map</param>
            /// <param name="callback" type="Function">A callback function to execute when the connection has started</param>
            /// <returns type="signalR" />
            var connection = this,
                config = {
                    transport: "auto"
                },
                initialize;

            if (connection.transport) {
                // Already started, just return
                return connection;
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

            if ($.type(callback) === "function") {
                $(connection).bind("onStart", function (e, data) {
                    callback.call(connection);
                });
            }

            initialize = function (transports, index) {
                index = index || 0;
                if (index >= transports.length) {
                    if (!connection.transport) {
                        // No transport initialized successfully
                        throw "SignalR: No transport could be initialized successfully. Try specifying a different transport or none at all for auto initialization.";
                    }
                    return;
                }

                var transportName = transports[index],
                    transport = $.type(transportName) === "object" ? transportName : signalR.transports[transportName];

                transport.start(connection, function () {
                    connection.transport = transport;
                    $(connection).trigger("onStart");
                }, function () {
                    initialize(transports, index + 1);
                });
            };

            window.setTimeout(function () {
                $.ajax(connection.url + '/negotiate', {
                    global: false,
                    type: "POST",
                    data: {},
                    success: function (res) {
                        connection.appRelativeUrl = res.Url;
                        connection.id = res.ConnectionId;

                        $(connection).trigger("onStarting");

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

            return connection;
        },

        starting: function (callback) {
            /// <summary>Adds a callback that will be invoked before the connection is started</summary>
            /// <param name="callback" type="Function">A callback function to execute when the connection is starting</param>
            /// <returns type="signalR" />
            var connection = this,
                $connection = $(connection);

            $connection.bind("onStarting", function (e, data) {
                callback.call(connection);
                // Unbind immediately, we don't want to call this callback again
                $connection.unbind("onStarting");
            });

            return connection;
        },

        send: function (data) {
            /// <summary>Sends data over the connection</summary>
            /// <param name="data" type="String">The data to send over the connection</param>
            /// <returns type="signalR" />
            var connection = this;

            if (!connection.transport) {
                // Connection hasn't been started yet
                throw "SignalR: Connection must be started before data can be sent. Call .start() before .send()";
            }

            connection.transport.send(connection, data);

            return connection;
        },

        sending: function (callback) {
            /// <summary>Adds a callback that will be invoked before anything is sent over the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute before each time data is sent on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind("onSending", function (e, data) {
                callback.call(connection);
            });
            return connection;
        },

        received: function (callback) {
            /// <summary>Adds a callback that will be invoked after anything is received over the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute when any data is received on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind("onReceived", function (e, data) {
                callback.call(connection, data);
            });
            return connection;
        },

        error: function (callback) {
            /// <summary>Adds a callback that will be invoked after an error occurs with the connection</summary>
            /// <param name="callback" type="Function">A callback function to execute when an error occurs on the connection</param>
            /// <returns type="signalR" />
            var connection = this;
            $(connection).bind("onError", function (e, data) {
                callback.call(connection);
            });
            return connection;
        },

        stop: function () {
            /// <summary>Stops listening</summary>
            /// <returns type="signalR" />
            var connection = this;

            if (connection.transport) {
                connection.transport.stop(connection);
                connection.transport = null;
            }

            delete connection.messageId;
            delete connection.groups;

            return connection;
        }
    };

    signalR.fn.init.prototype = signalR.fn;

    // Transports
    var transportLogic = {

        getUrl: function (connection, transport) {
            /// <summary>Gets the url for making a GET based connect request</summary>
            var url = connection.url + "/connect",
                qs = "transport=" + transport + "&connectionId=" + window.escape(connection.id);
            if (connection.data) {
                qs = "connectionData=" + window.escape(connection.data) + "&" + qs;
            }
            url += "?" + qs;
            return url;
        },

        ajaxSend: function (connection, data) {
            $.ajax(connection.url + "/send" + "?transport=" + connection.transport.name + "&connectionId=" + window.escape(connection.id), {
                global: false,
                type: "POST",
                dataType: "json",
                data: {
                    data: data
                },
                success: function (result) {
                    if (result) {
                        $(connection).trigger("onReceived", [result]);
                    }
                },
                error: function (errData, textStatus) {
                    if (textStatus === "abort") {
                        return;
                    }
                    $(connection).trigger("onError", [errData]);
                }
            });
        },

        processMessages: function (connection, data) {
            if (data) {
                if (data.Disconnect) {
                    // Disconnected by the server, need to reconnect
                    connection.stop()
                        .start();
                    return;
                }

                if (data.Messages) {
                    $.each(data.Messages, function () {
                        try {
                            $(connection).trigger("onReceived", [this]);
                        }
                        catch (e) {
                            log('Error raising received ' + e);
                        }
                    });
                }
                connection.messageId = data.MessageId;
                connection.groups = data.TransportData.Groups;
            }
        },

        foreverFrame: {
            count: 0,
            connections: {}
        }
    };

    signalR.transports = {

        webSockets: {
            name: "webSockets",

            send: function (connection, data) {
                connection.socket.send(data);
            },

            start: function (connection, onSuccess, onFailed) {
                var url,
                    opened = false,
                    protocol;

                if (window.MozWebSocket) {
                    window.WebSocket = window.MozWebSocket;
                }

                if (!window.WebSocket) {
                    onFailed();
                    return;
                }

                if (!connection.socket) {
                    // Build the url
                    url = document.location.host + connection.appRelativeUrl;

                    $(connection).trigger("onSending");
                    if (connection.data) {
                        url += "?connectionData=" + connection.data + "&transport=webSockets&connectionId=" + connection.id;
                    } else {
                        url += "?transport=webSockets&connectionId=" + connection.id;
                    }

                    protocol = document.location.protocol === "https:" ? "wss://" : "ws://";

                    connection.socket = new window.WebSocket(protocol + url);
                    connection.socket.onopen = function () {
                        opened = true;
                        if (onSuccess) {
                            onSuccess();
                        }
                    };

                    connection.socket.onclose = function (event) {
                        if (!opened) {
                            if (onFailed) {
                                onFailed();
                            }
                        } else if (typeof event.wasClean != 'undefined' && event.wasClean === false) {
                            // Ideally this would use the websocket.onerror handler (rather than checking wasClean in onclose) but
                            // I found in some circumstances Chrome won't call onerror. This implementation seems to work on all browsers.
                            $(connection).trigger('onError');
                            // TODO: Support reconnect attempt here, need to ensure last message id, groups, and connection data go up on reconnect
                        }
                        connection.socket = null;
                    };

                    connection.socket.onmessage = function (event) {
                        var data = window.JSON.parse(event.data);
                        if (data) {
                            if (data.Messages) {
                                $.each(data.Messages, function () {
                                    $(connection).trigger("onReceived", [this]);
                                });
                            } else {
                                $(connection).trigger("onReceived", [data]);
                            }
                        }
                    };
                }
            },

            stop: function (connection) {
                if (connection.socket !== null) {
                    connection.socket.close();
                    connection.socket = null;
                }
            }
        },

        serverSentEvents: {
            name: "serverSentEvents",

            start: function (connection, onSuccess, onFailed) {
                var that = this,
                    opened = false,
                    url;

                if (connection.eventSource) {
                    connection.stop();
                }

                if (!window.EventSource) {
                    onFailed();
                    return;
                }

                $(connection).trigger("onSending");

                url = transportLogic.getUrl(connection, this.name);

                connection.eventSource = new window.EventSource(url);

                connection.eventSource.addEventListener("open", function (e) {
                    // opened
                    opened = true;
                    onSuccess();
                }, false);

                connection.eventSource.addEventListener("message", function (e) {
                    // process messages
                    //log("SignalR: EventSource message received - " + e.data);
                    if (e.data === "initialized") {
                        return;
                    }
                    var data = window.JSON.parse(e.data);
                    transportLogic.processMessages(connection, data);
                    // TODO: persist the groups and connection data in a cookie
                }, false);

                connection.eventSource.addEventListener("error", function (e) {
                    if (!opened) {
                        onFailed();
                    }
                    if (e.eventPhase == EventSource.CLOSED) {
                        // connection closed
                        log("SignalR: EventSource closed");
                        that.stop();
                    } else {
                        // connection error
                        log("SignalR: EventSource error");
                        $(instance).trigger("onError", [data]);
                    }
                }, false);
            },

            send: function (connection, data) {
                transportLogic.ajaxSend(connection, data);
            },

            stop: function (connection) {
                if (connection && connection.eventSource) {
                    connection.eventSource.close();
                    connection.eventSource = null;
                }
            }
        },

        foreverFrame: {
            name: "foreverFrame",

            start: function (connection, onSuccess, onFailed) {
                var that = this,
                    frameId = (transportLogic.foreverFrame.count += 1),
                    url,
                    frame = $("<iframe data-signalr-connection-id='" + connection.id + "' style='position:absolute;width:0;height:0;visibility:hidden;'></iframe>");

                $(connection).trigger("onSending");

                // Build the url
                url = transportLogic.getUrl(connection, this.name);
                url += "&frameId=" + frameId;

                frame.prop("src", url);
                transportLogic.foreverFrame.connections[frameId] = connection;

                frame.bind("load", function () {
                    log("SignalR: forever frame iframe load event fired");
                    that.reconnect(connection);
                });

                connection.frame = frame[0];
                connection.frameId = frameId;

                if (onSuccess) {
                    connection.onSuccess = onSuccess;
                }

                $("body").append(frame);
            },

            reconnect: function (connection) {
                window.setTimeout(function () {
                    var frame = connection.frame,
                        src = frame.src.replace("/connect", "") + "&messageId=" + connection.messageId + "&groups=" + escape(connection.groups);
                    frame.src = src;
                }, 2000);
            },

            send: function (connection, data) {
                transportLogic.ajaxSend(connection, data);
            },

            receive: transportLogic.processMessages,

            stop: function (connection) {
                if (connection.frame) {
                    connection.frame.remove();
                    delete transportLogic.foreverFrame.connections[connection.frameId];
                }
            },

            getConnection: function (id) {
                return transportLogic.foreverFrame.connections[id];
            },

            started: function (connection) {
                if (connection.onSuccess) {
                    connection.onSuccess();
                }
            }
        },

        longPolling: {
            name: "longPolling",

            start: function (connection, onSuccess, onFailed) {
                /// <summary>Starts the long polling connection</summary>
                /// <param name="connection" type="signalR">The SignalR connection to start</param>
                if (connection.pollXhr) {
                    connection.stop();
                }

                connection.messageId = null;

                window.setTimeout(function () {
                    (function poll(instance) {
                        $(instance).trigger("onSending");

                        var messageId = instance.messageId,
                            connect = (messageId === null),
                            url = instance.url + (connect ? "/connect" : "") +
                                "?transport=longPolling" +
                                "&connectionId=" + escape(instance.id) +
                                "&messageId=" + messageId +
                                "&groups=" + escape((instance.groups || []).toString());

                        instance.pollXhr = $.ajax(url, {
                            global: false,

                            type: "POST",

                            data: {
                                connectionData: instance.data
                            },

                            dataType: "json",

                            success: function (data) {
                                var delay = 0;
                                transportLogic.processMessages(instance, data);
                                if (data && $.type(data.TransportData.LongPollDelay) === "number") {
                                    delay = data.TransportData.LongPollDelay;
                                }
                                if (delay > 0) {
                                    window.setTimeout(function () {
                                        poll(instance);
                                    }, delay);
                                } else {
                                    poll(instance);
                                }
                            },

                            error: function (data, textStatus) {
                                if (textStatus === "abort") {
                                    return;
                                }

                                $(instance).trigger("onError", [data]);

                                window.setTimeout(function () {
                                    poll(instance);
                                }, 2 * 1000);
                            }
                        });
                    } (connection));

                    // Now connected
                    // There's no good way know when the long poll has actually started so 
                    // we and assume it only takes around 150ms (max) to start connection 
                    // to start.
                    setTimeout(onSuccess, 150);

                }, 250); // Have to delay initial poll so Chrome doesn't show loader spinner in tab
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
                }
            }
        }
    };

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

} (window.jQuery, window));