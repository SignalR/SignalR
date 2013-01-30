var testUtilities;

(function ($, window) {
    var ios = !!((navigator.userAgent.match(/iPhone/i)) || (navigator.userAgent.match(/iPod/i))),
        ios6 = !!(ios && navigator.userAgent.indexOf("OS 6_0") >= 0),
        // Checks if the transport (string) is enabled
        transportEnabled = function (transport) {
            var property = transport + "Enabled";

            if (typeof (this[property]) !== "undefined") {
                return this[property];
            }

            throw new Error("Invalid Transport");
        };

    testUtilities = {
        transportNames: ["longPolling", "foreverFrame", "serverSentEvents", "webSockets"],
        runWithAllTransports: function (test) {
            this.runWithTransports(this.transportNames, test);
        },
        runWithTransports: function (transports, test) {
            $.each(transports, function (_, transport) {
                if (transportEnabled(transport)) {
                    test(transport);
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
        },
        webSocketsEnabled: (function () {
            var validPlatform = true;

            if (ios && !ios6) {
                validPlatform = false;
            }

            return !!(window.WebSocket && !window.document.commandLineTest && validPlatform);
        })(),
        foreverFrameEnabled: (function () {
            return !window.EventSource && !window.document.commandLineTest && navigator.appName === "Microsoft Internet Explorer";
        })(),
        serverSentEventsEnabled: (function () {
            return !!window.EventSource;
        })(),
        longPollingEnabled: (function () {
            return true;
        })()
    };
})($, window);