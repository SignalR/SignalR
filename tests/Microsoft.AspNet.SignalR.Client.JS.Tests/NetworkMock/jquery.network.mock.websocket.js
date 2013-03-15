﻿// Web Socket network mock
(function ($, window, undefined) {
    var enabled = !!window.WebSocket,
        savedWebSocket = window.WebSocket,
        network = $.network,
        webSocketData = {},
        webSocketIds = 0,
        ignoringMessages = false,
        fail = function (data, soft) {
            // Used to not trigger any methods from a resultant web socket completion event.
            ignoringMessages = true;
            data.close();
            ignoringMessages = false;

            if (!soft) {
                data.onclose({});
            }
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
                    delete webSocketData[id];
                    return ws.close();
                });
            };

            that.send = function () {
                var args = arguments;

                tryExecute(function () {
                    if (!ignoringMessages) {
                        return ws.send.apply(ws, args)
                    }
                    else {
                        // If we're trying ot send while the network is down then we need to fail.
                        // Act async for failure of request
                        setTimeout(function () {
                            fail(webSocketData[id], true);
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
                    if (!ignoringMessages) {
                        return that.onopen.apply(that, arguments);
                    }
                };
                ws.onmessage = function () {
                    if (!ignoringMessages) {
                        return that.onmessage.apply(that, arguments);
                    }
                };
                ws.onclose = function () {
                    if (!ignoringMessages) {
                        return that.onclose.apply(that, arguments);
                    }

                    delete webSocketData[id];
                };

                webSocketData[id] = that;

                // Cycle through queued commands and execute them all
                while(queued.length > 0) {
                    queued.shift()();
                }
            }, 0);
        };

        window.WebSocket = CustomWebSocket;
    }

    network.websocket = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            for (var key in webSocketData) {
                var data = webSocketData[key];

                fail(data, soft);
            }
            ignoringMessages = true;
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
