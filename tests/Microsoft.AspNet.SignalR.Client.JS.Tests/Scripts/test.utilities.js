var testUtilities;

(function ($, window) {
    var ios = !!((navigator.userAgent.match(/iPhone/i)) || (navigator.userAgent.match(/iPod/i))),
        ios6 = !!(ios && navigator.userAgent.indexOf("OS 6_0") >= 0);

    testUtilities = {
        transports: {
            longPolling: {
                enabled: true
            },
            foreverFrame: {
                enabled: !window.EventSource && !window.document.commandLineTest && navigator.appName === "Microsoft Internet Explorer"
            },
            serverSentEvents: {
                enabled: !!window.EventSource
            },
            webSockets: {
                enabled: !!(window.WebSocket && !window.document.commandLineTest && (!ios || ios6))
            }
        },
        transportNames: null, // This is set after the initial creation
        runWithAllTransports: function (fn) {
            this.runWithTransports(this.transportNames, fn);
        },
        runWithTransports: function (transports, fn) {
            $.each(transports, function (_, transport) {
                if (transportEnabled(transport)) {
                    fn(transport);
                }
            });
        },
        defaultTestTimeout: (function () {
            var defaultTestTimeout = window.location.href.match(/#defaultTestTimeout=\d+/g);
            
            // There is no default test timeout parameter
            if (!defaultTestTimeout) {
                // Return the default
                return 10000;
            }
            
            // Match returns an array, so we need to take the first element (string).
            defaultTestTimeout = defaultTestTimeout[0].match(/\d+/g);
            // Convert to integer and translate to milliseconds
            defaultTestTimeout = parseInt(defaultTestTimeout) * 1000;

            return defaultTestTimeout;
        })(),
        createHubConnection: function (testName) {
            var connection,
                qs = (testName ? "test=" + window.encodeURIComponent(testName) : "");

            if (window.document.testUrl !== 'auto') {
                connection = $.hubConnection(window.document.testUrl, { qs: qs });
            } else {
                connection = $.hubConnection('signalr', { useDefaultPath: false, qs: qs });
            }

            connection.logging = true;

            return connection;
        },
        createConnection: function (url, testName) {
            var connection,
                qs = (testName ? "test=" + window.encodeURIComponent(testName) : "");

            if (window.document.testUrl !== 'auto') {
                connection = $.connection(window.document.testUrl + '/' + url, qs);
            } else {
                connection = $.connection(url, qs);
            }

            connection.logging = true;

            return connection;
        }
    };

    // Create the transport names based off of the transport list
    testUtilities.transportNames = $.map(testUtilities.transports, function (_, transportName) {
        return transportName;
    });
})($, window);