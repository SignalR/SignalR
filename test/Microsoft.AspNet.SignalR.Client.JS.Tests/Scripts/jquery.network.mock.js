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
        enable: function () {
            /// <summary>Enables the network mock functionality.</summary>
            invoke("enable");
        },
        disable: function () {
            /// <summary>Disables the network mock functionality.</summary>
            invoke("disable");
        },
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
        modifiedAjax,
        network = $.network,
        ajaxData = {},
        ajaxIds = 0,
        ignoringMessages = false,
        failSoftly = false,
        disableAbortError = false,
        fail = function (id) {
            var data = ajaxData[id];
            if (data) {
                // We must close the request prior to calling error so that we can pass our own
                // reason for failing the request.
                disableAbortError = true;
                data.Request.abort();
                disableAbortError = false;

                data.Settings.error({ responseText: "NetworkMock: Disconnected." });

                delete ajaxData[id];
            }
        };

    function failAllOngoingRequests() {
        $.each(ajaxData, function (id, _) {
            fail(id);
        });
    }

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
            if (!ignoringMessages && ajaxData[id]) {
                delete ajaxData[id];
                if (savedSuccess) {
                    savedSuccess.apply(this, arguments);
                }
            }
        }

        settings.error = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (ajaxData[id] && !disableAbortError) {
                delete ajaxData[id];
                if (!failSoftly) {
                    if (savedError) {
                        savedError.apply(this, arguments);
                    }
                }
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
                fail(id);
            }, 0);
        }

        return request;
    };

    modifiedAjax = $.ajax;

    network.ajax = {
        enable: function () {
            /// <summary>Enables the Ajax network mock functionality.</summary>
            $.ajax = modifiedAjax;
        },
        disable: function () {
            /// <summary>Disables the Ajax network mock functionality.</summary>
            $.ajax = savedAjax;
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling failAllOngoingRequests,
            // because we might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            failSoftly = soft === true;
            failAllOngoingRequests();
            failSoftly = false;
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>

            // fail all ongoing requests synchronously so we don't have to worry about any requests failing unnecessarily after connect completes.
            failAllOngoingRequests();
            ignoringMessages = false;
        }
    };
})($, window);
/* jquery.network.mock.websocket.js */
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
/* jquery.network.mock.mask.js */
// Masks network mock
(function ($, window) {
    var network = $.network,
        maskData = {},
        controlData = [],
        maskIds = 0,
        ignoringMessages = false;

    network.mask = {
        create: function (maskBase, onErrorProperties, subscriptionProperties, destroyOnError) {
            /// <summary>Wires the maskBase's onError and onMessage properties to be affected by network loss</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by onErrorProperty and onMessageProperty</param>
            /// <param name="onErrorProperties" type="Array">The array of string representation of the onError methods of the maskBase</param>
            /// <param name="subscriptionProperties" type="Array">The array of string representation of the methods subscribed to network events on the maskBase</param>
            /// <param name="destroyOnError" type="Boolean">Represents if we destroy our created mask object onError.</param>
            var savedOnErrors = {},
                savedSubscriptions = {},
                modifiedOnErrors = {},
                modifiedSubscriptions = {},
                id = maskIds++;

            function disable() {
                $.extend(maskBase, savedOnErrors);
                $.extend(maskBase, savedSubscriptions);
            }

            function enable() {
                $.extend(maskBase, modifiedOnErrors);
                $.extend(maskBase, modifiedSubscriptions);
            }

            maskData[id] = {
                onError: [],
                onMessage: []
            };

            $.each(onErrorProperties, function (_, onErrorProperty) {
                var saved = maskBase[onErrorProperty];

                if (!saved) {
                    throw new Error("maskBase must have each specified onError property.");
                }

                maskBase[onErrorProperty] = function () {
                    if (destroyOnError) {
                        disable();
                        delete maskData[id];
                    }

                    return saved.apply(this, arguments);
                }

                maskData[id].onError.push(maskBase[onErrorProperty]);
                savedOnErrors[onErrorProperty] = saved;
                modifiedOnErrors[onErrorProperty] = maskBase[onErrorProperty];
            });

            
            $.each(subscriptionProperties, function (_, subscriptionProperty) {
                var saved = maskBase[subscriptionProperty];

                if (!saved) {
                    throw new Error("maskBase must have each specified subscription property.");
                }

                maskBase[subscriptionProperty] = function () {
                    if (!ignoringMessages) {
                        return saved.apply(this, arguments);
                    }
                };

                maskData[id].onMessage.push(maskBase[subscriptionProperty]);
                savedSubscriptions[subscriptionProperty] = saved;
                modifiedSubscriptions[subscriptionProperty] = maskBase[subscriptionProperty];
            });

            controlData.push({
                enable: function () {
                    enable();
                },
                disable: function () {
                    disable();
                }
            });

            return controlData[controlData.length-1];
        },
        subscribe: function (maskBase, functionName, onBadAttempt) {
            /// <summary>Subscribes a function to only be called when the network is up</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by functionName</param>
            /// <param name="functionName" type="String">Function name that should be replaced on the maskBase</param>
            /// <param name="onBadAttempt" type="Function">Function to only be called when an attempt to call the functionName function is called when network is down</param>
            /// <returns type="Object">Object with an unsubscribe method used to remove the network connection monitoring.</returns>
            var savedFunction = maskBase[functionName],
                modifiedFunction;

            maskBase[functionName] = function () {
                if (!ignoringMessages) {
                    return savedFunction.apply(this, arguments);
                }
                else if (onBadAttempt) {
                    onBadAttempt();
                }
            };

            modifiedFunction = maskBase[functionName];

            controlData.push({
                enable: function () {
                    maskBase[functionName] = modifiedFunction;
                },
                disable: function () {
                    maskBase[functionName] = savedFunction;
                }
            });

            return controlData[controlData.length - 1]
        },
        enable: function () {
            /// <summary>Enables the Mask network mock functionality.</summary>
            for (var i = 0; i < controlData.length; i++) {
                controlData[i].enable();
            }
        },
        disable: function () {
            /// <summary>Disables the Mask network mock functionality.</summary>
            for (var i = 0; i < controlData.length; i++) {
                controlData[i].disable();
            }
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling errorFunc, because we
            // might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            if (!soft) {
                $.each(maskData, function (_, data) {
                    $.each(data.onError, function (_, errorFunc) {
                        errorFunc();
                    });
                });
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
(function ($, window, undefined) {
    var enabled = !!window.EventSource,
        savedEventSource = window.EventSource,
        modifiedEventSource,
        network = $.network,
        eventSourceData = {},
        eventSourceIds = 0,
        ignoringMessages = false;

    if (enabled) {
        function CustomEventSource(url, eventSourceInit) {
            var es,
                that = this,
                id = eventSourceIds++;

            if (eventSourceInit === undefined) {
                es = new savedEventSource(url);
            } else {
                es = new savedEventSource(url, eventSourceInit);
            }

            that._events = {};

            that.addEventListener = function (name, event) {
                var isError = name === "error",
                    fn = function () {
                        if (eventSourceData[id]) {
                            if (isError) {
                                delete eventSourceData[id];
                                return event.apply(that, arguments);
                            } else if (!ignoringMessages) {
                                return event.apply(that, arguments);
                            }
                        }
                    };

                that._events[name] = fn;
                es.addEventListener(name, fn);

                if (ignoringMessages && isError) {
                    // We don't want to call the error listener synchronously
                    setTimeout(function () {
                        fn({ eventPhase: savedEventSource.CLOSED });
                    }, 0);
                }
            };

            that.close = function () {
                that._events = null;
                delete eventSourceData[id];
                return es.close();
            };

            eventSourceData[id] = that;
        };

        // Copy constants like CLOSED, CONNECTING and OPEN
        $.extend(CustomEventSource, window.EventSource);

        window.EventSource = CustomEventSource;
        modifiedEventSource = CustomEventSource;
    }

    network.eventsource = {
        enable: function () {
            /// <summary>Enables the EventSource network mock functionality.</summary>
            if (enabled) {
                window.EventSource = modifiedEventSource;
            }
        },
        disable: function () {
            /// <summary>Disables the EventSource network mock functionality.</summary>
            if (enabled) {
                window.EventSource = savedEventSource;
            }
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling data._events.error,
            // because we might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            if (!soft) {
                for (var key in eventSourceData) {
                    var data = eventSourceData[key];

                    if (typeof data._events.error === "function") {
                        data._events.error({ eventPhase: savedEventSource.CLOSED });
                    }

                    // Used to not trigger any methods from a resultant event source completion event.
                    data.close();
                }
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
