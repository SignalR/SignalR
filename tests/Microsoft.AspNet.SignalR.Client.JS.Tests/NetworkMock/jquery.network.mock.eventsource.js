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
