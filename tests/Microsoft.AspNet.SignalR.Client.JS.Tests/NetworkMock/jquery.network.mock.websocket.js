// Web Socket network mock
(function ($, window, undefined) {
    var enabled = !!window.WebSocket,
        savedWebSocket = window.WebSocket,
        modifiedWebSocket,
        network = $.network,
        webSocketData = {},
        webSocketIds = 0,
        ignoringMessages = false,
        fail = function (data) {
            data.onclose({});
        };

    if (enabled) {
        function CustomWebSocket(url, webSocketInit) {
            var ws,
                that = this,
                id = webSocketIds++,
                queued = [],
                tryExecute = function (fn) {
                    // If we haven't created the websocket yet, we need to queue the execution.
                    if (!ws) {
                        queued.push(fn);
                    }
                    else {
                        fn();
                    }
                };

            // These are usually set after creating a WebSocket
            that.onmessage;
            that.onclose;
            that.onopen;

            that.close = function () {
                tryExecute(function () {
                    return ws.close();
                });
            };

            that.send = function () {
                var args = arguments;

                tryExecute(function () {
                    if (!ignoringMessages && webSocketData[id]) {
                        return ws.send.apply(ws, args)
                    }
                    else {
                        // If we're trying ot send while the network is down then we need to fail.
                        // Act async for failure of request
                        setTimeout(function () {
                            fail(webSocketData[id]);
                        }, 0);
                    }
                });
            };

            webSocketData[id] = that;

            // Letting current running context finish before building the websocket.
            // This way we can patch every function that was set.
            setTimeout(function () {
                if (webSocketInit === undefined) {
                    ws = new savedWebSocket(url);
                } else {
                    ws = new savedWebSocket(url, webSocketInit);
                }

                ws.onopen = function () {
                    if (!ignoringMessages && webSocketData[id]) {
                        return that.onopen.apply(that, arguments);
                    }
                };
                ws.onmessage = function () {
                    if (!ignoringMessages && webSocketData[id]) {
                        return that.onmessage.apply(that, arguments);
                    }
                };
                ws.onclose = function () {
                    if (webSocketData[id]) {
                        delete webSocketData[id];
                        return that.onclose.apply(that, arguments);
                    }
                };

                if (ignoringMessages) {
                    fail(that);
                }

                // Cycle through queued commands and execute them all
                while(queued.length > 0) {
                    queued.shift()();
                }
            }, 0);
        };

        // Copy constants like CLOSED, CLOSING, CONNECTING and OPEN
        $.extend(CustomWebSocket, window.WebSocket);

        // Remove the WebSocket prototype otherwise Safari 7.1+ dies
        CustomWebSocket.prototype = null;

        window.WebSocket = CustomWebSocket;
        modifiedWebSocket = CustomWebSocket;
    }

    network.websocket = {
        enable: function () {
            /// <summary>Enables the WebSocket network mock functionality.</summary>
            if (enabled) {
                window.WebSocket = modifiedWebSocket;
            }
        },
        disable: function () {
            /// <summary>Disables the WebSocket network mock functionality.</summary>
            if (enabled) {
                window.WebSocket = savedWebSocket;
            }
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling fail, because we 
            // might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            if (!soft) {
                for (var key in webSocketData) {
                    fail(webSocketData[key]);
                    delete webSocketData[key];
                }
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
