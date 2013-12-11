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
