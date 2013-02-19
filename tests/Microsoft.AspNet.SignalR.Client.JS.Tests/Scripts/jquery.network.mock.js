/* jquery.network.mock.core.js */
/// <reference path="jquery.network.mock.ajax.js" />
/// <reference path="jquery.network.mock.eventsource.js" />
/// <reference path="jquery.network.mock.mask.js" />
/// <reference path="jquery.network.mock.websocket.js" />

// Network core
(function ($, window) {
    var invoke = function (methodName) { // Invoke the method with the name methodName on all mocks
        var args = Array.prototype.slice.call(arguments);
        args.splice(0, 1);

        // Declared array here because at initial core creation the $.network.X does not exist.
        $.each([$.network.ajax, $.network.eventsource, $.network.mask, $.network.websocket], function () {
            this[methodName].apply(this, args);
        });
    },
    network = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            invoke("disconnect", soft);
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            invoke("connect");
        }
    };

    $.network = network;
})($, window);
/* jquery.network.mock.ajax.js */
// Ajax network mock
(function ($, window) {
    var savedAjax = $.ajax,
        network = $.network,
		ajaxData = {},
		ajaxIds = 0,
        ignoringMessages = false,
        fail = function (data, soft) {
            // We must close the request prior to calling error so that we can pass our own
            // reason for failing the request.
            ignoringMessages = true;
            data.Request.abort();
            ignoringMessages = false;

            if (!soft) {
                data.Settings.error(data, "error");
            }
        };

    $.ajax = function (url, settings) {
        var request,
			savedSuccess,
			savedError,
			id = ajaxIds++;

        if (!settings) {
            settings = url;
        }

        savedSuccess = settings.success;
        savedError = settings.error;

        settings.success = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (ignoringMessages === false) {
                if (savedSuccess) {
                    savedSuccess.apply(this, arguments);
                }

                delete ajaxData[id];
            }
        }

        settings.error = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (ignoringMessages === false) {
                if (savedError) {
                    savedError.apply(this, arguments);
                }

                delete ajaxData[id];
            }
        }

        request = savedAjax.apply(this, [url, settings]);
        ajaxData[id] = {
            Request: request,
            Settings: settings
        };

        // If we're trying to make an ajax request while the network is down
        if (ignoringMessages) {
            // Act async for failure of request
            setTimeout(function () {
                fail(ajaxData[id], false);
            }, 0);
        }

        return request;
    };

    network.ajax = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            for (var key in ajaxData) {
                var data = ajaxData[key];

                fail(data, soft);
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
/* jquery.network.mock.websocket.js */
// Web Socket network mock
(function ($, window) {
    var enabled = !!window.WebSocket,
        savedWebSocket = enabled ? WebSocket : {},
        network = $.network,
        webSocketData = {},
        webSocketIds = 0,
        ignoringMessages = false,
        fail = function (data) {
            // Used to not trigger any methods from a resultant web socket completion event.
            ignoringMessages = true;
            data.close();
            ignoringMessages = false;

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
                if (!ignoringMessages) {
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
                ignoringMessages = true;
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
/* jquery.network.mock.mask.js */
// Masks network mock
(function ($, window) {
    var network = $.network,
        maskData = {},
        maskIds = 0,
        ignoringMessages = false;

    network.mask = {
        create: function (maskBase, onErrorProperty, onMessageProperty, destroyOnError) {
            /// <summary>Wires the maskBase's onError and onMessage properties to be affected by network loss</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by onErrorProperty and onMessageProperty</param>
            /// <param name="onErrorProperty" type="String">The string representation of the onError method of the maskBase</param>
            /// <param name="onMessageProperty" type="String">The string representation of the onMessage method of the maskBase</param>
            /// <param name="destroyOnError" type="Boolean">Represents if we destroy our created mask object onError.</param>
            var savedOnError = maskBase[onErrorProperty],
                savedOnMessage = maskBase[onMessageProperty],
                id = maskIds++;

            if (!savedOnError || !savedOnMessage) {
                throw new Error("maskBase must have an onErrorProperty AND an onMessageProperty property");
            }

            maskBase[onErrorProperty] = function () {
                if (!ignoringMessages) {
                    return savedOnError.apply(this, arguments);
                }

                if (destroyOnError) {
                    // Reset the maskBase to its original setup
                    maskBase[onErrorProperty] = savedOnError;
                    maskBase[onMessageProperty] = savedOnMessage;
                    delete maskData[id];
                }
            };

            maskBase[onMessageProperty] = function () {
                if (!ignoringMessages) {
                    return savedOnMessage.apply(this, arguments);
                }
            };

            maskData[id] =
            {
                onError: maskBase[onErrorProperty],
                onMessage: maskBase[onMessageProperty]
            };
        },
        subscribe: function (maskBase, functionName, onBadAttempt) {
            /// <summary>Subscribes a function to only be called when the network is up</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by functionName</param>
            /// <param name="functionName" type="String">Function name that should be replaced on the maskBase</param>
            /// <param name="onBadAttempt" type="Function">Function to only be called when an attempt to call the functionName function is called when network is down</param>
            /// <returns type="Object">Object with an unsubscribe method used to remove the network connection monitoring.</returns>
            var savedFunction = maskBase[functionName];

            maskBase[functionName] = function () {
                if (!ignoringMessages) {
                    return savedFunction.apply(this, arguments);
                }
                else if (onBadAttempt) {
                    onBadAttempt();
                }
            };

            return {
                unsubscribe: function () {
                    maskBase[functionName] = savedFunction;
                }
            };
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            if (!soft) {
                for (var key in maskData) {
                    var data = maskData[key];

                    data.onError();
                }
            }
            else {
                ignoringMessages = true;
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
/* jquery.network.mock.eventsource.js */
// Event Source network mock
(function ($, window) {
    var enabled = !!window.EventSource,
        savedEventSource = enabled ? EventSource : {},
        network = $.network,
        eventSourceData = {},
        eventSourceIds = 0,
        ignoringMessages = false;

    if (enabled) {
        function CustomEventSource(url, eventSourceInit) {
            var es = new savedEventSource(url, eventSourceInit),
                that = this,
                id = eventSourceIds++;

            that._events = {};

            that.addEventListener = function (name, event) {
                var fn = function () {
                    if (!ignoringMessages) {
                        return event.apply(this, arguments);
                    }
                };

                that._events[name] = fn;
                es.addEventListener(name, fn);
            };

            that.close = function () {
                that._events = null;
                delete eventSourceData[id];
                return es.close();
            };

            eventSourceData[id] = that;
        };

        EventSource = CustomEventSource;
    }

    network.eventsource = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            if (!soft) {
                for (var key in eventSourceData) {
                    var data = eventSourceData[key];

                    data._events.error.call(data, savedEventSource.CLOSED);

                    // Used to not trigger any methods from a resultant event source completion event.
                    ignoringMessages = true;
                    data.close();
                    ignoringMessages = false;
                }
            }
            else {
                ignoringMessages = true;
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
