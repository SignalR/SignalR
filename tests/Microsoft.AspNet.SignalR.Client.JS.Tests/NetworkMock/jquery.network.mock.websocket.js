// Web Socket network mock
(function ($, window) {
    var enabled = !!window.WebSocket,
        savedWebSocket = enabled ? WebSocket : {},
        network = $.network,
        webSocketData = {},
        webSocketIds = 0,
        sleeping = false,
        fail = function (data) {
            // Used to not trigger any methods from a resultant web socket completion event.
            sleeping = true;
            data.close();
            sleeping = false;

            data.onclose({});
        };

    if (enabled) {
        function CustomWebSocket(url, webSocketInit) {
            var ws,
                that = this,
                id = webSocketIds++;

            // These are usually set after creating a WebSocket
            that.onmessage;
            that.onclose;
            that.onopen;

            that.close = function () {
                return ws.close();
            };

            that.send = function () {
                if (!sleeping) {
                    return ws.send.apply(ws, arguments)
                }
                else {
                    // If we're trying ot send while the network is down then we need to fail.
                    // Act async for failure of request
                    setTimeout(function () {
                        fail(webSocketData[id]);
                    }, 0);
                }

            };

            webSocketData[id] = that;

            // Letting current running context finish before building the websocket.
            // This way we can patch every function that was set.
            setTimeout(function () {
                ws = new savedWebSocket(url, webSocketInit);
                ws.onopen = function () {
                    if (!sleeping) {
                        return that.onopen.apply(that, arguments);
                    }
                };
                ws.onmessage = function () {
                    if (!sleeping) {
                        return that.onmessage.apply(that, arguments);
                    }
                };
                ws.onclose = function () {
                    if (!sleeping) {
                        return that.onclose.apply(that, arguments);
                    }

                    delete webSocketData[id];
                };

                webSocketData[id] = that;
            }, 0);
        };

        WebSocket = CustomWebSocket;
    }

    network.websocket = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            if (!soft) {
                for (var key in webSocketData) {
                    var data = webSocketData[key];

                    fail(data);
                }
            }
            else {
                sleeping = true;
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            sleeping = false;
        }
    };
})($, window);
