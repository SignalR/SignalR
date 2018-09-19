var testUtilities;

// Clear session storage so QUnit does not try to re-run failed tests first.
window.sessionStorage.clear();

(function ($, window) {
    var ios = !!navigator.userAgent.match(/iPod|iPhone|iPad/),
        rfcWebSockets = !!window.WebSocket,
        iosVersion;

    function wrapConnectionStart(connection, end, assert, ignoreErrors) {
        var savedConnectionStart = connection.start;

        connection.start = function () {
            return savedConnectionStart.apply(connection, arguments).fail(function (reason) {
                if (assert) {
                    assert.ok(false, "Failed to initiate signalr connection: " + window.JSON.stringify(reason));
                }
                if (end) {
                    end();
                }
            });
        }
    }

    if (ios && rfcWebSockets) {
        iosVersion = navigator.userAgent.match(/OS (\d+)/);
        if (iosVersion && iosVersion.length === 2) {
            rfcWebSockets = parseInt(iosVersion[1], 10) >= 6;
        }
    }

    testUtilities = {
        // Define a module that should be skipped on Azure SignalR
        module: function (name, condition) {
            if (typeof condition === "undefined" || condition === true) {
                QUnit.module(name);
            } else {
                QUnit.module.skip(name);
            }
        },
        transports: {
            longPolling: {
                enabled: true
            },
            foreverFrame: {
                enabled: !window.EventSource && !window.document.commandLineTest
            },
            serverSentEvents: {
                enabled: !!window.EventSource
            },
            webSockets: {
                enabled: !!(rfcWebSockets && !window.document.commandLineTest)
            }
        },
        transportNames: null, // This is set after the initial creation
        runWithAllTransports: function (fn) {
            this.runWithTransports(this.transportNames, fn);
        },
        runWithTransports: function (transports, fn) {
            $.each(transports, function (_, transport) {
                if (testUtilities.transports[transport].enabled) {
                    fn(transport);
                }
            });
        },
        theory: function (data, fn) {
            /// <summary>Executes fn using the data.</summary>
            /// <param name="data" type="Array">An array of objects containing the theory data.</param>
            /// <param name="fn" type="Function">The function to invoke for each theory data instance.</param>
            function executeWithArgs(argsObject, fn) {
                var args = [argsObject];

                if ($.type(argsObject) === "object") {
                    for (var name in argsObject) {
                        args.push(argsObject[name]);
                    }
                }

                fn.apply(undefined, args);
            }

            switch ($.type(data)) {
                case "object":
                    executeWithArgs(data, fn);
                    break;

                case "array":
                    $.each(data, function (_, d) {
                        executeWithArgs(d, fn);
                    });
                    break;

                default:
                    fn(data);
                    break;
            }
        },
        defaultTestTimeout: (function () {
            var defaultTestTimeout = window.location.search.match(/defaultTestTimeout=(\d+)/);

            // There is no default test timeout parameter
            if (!defaultTestTimeout) {
                // Return the default
                return 10000;
            }

            // Match returns an array, so we need to take the second element (string).
            defaultTestTimeout = defaultTestTimeout[1];
            // Convert to integer and translate to milliseconds
            defaultTestTimeout = parseInt(defaultTestTimeout, 10) * 1000;

            return defaultTestTimeout;
        })(),
        // A newer version of createHubConnection and createConnection
        createTestConnection: function (testName, end, assert, options) {
            if(typeof testName === "object") {
                options = testName;
                testName = undefined;
            }

            options = options || {};

            var connection,
                qs = (testName ? "test=" + window.encodeURIComponent(testName) : "");

            options.wrapStart = typeof options.wrapStart === "undefined" ? true : options.wrapStart;
            options.ignoreErrors = typeof options.ignoreErrors === "undefined" ? false : true;
            options.url = options.url ? options.url : "signalr";
            options.hub = typeof options.hub === "undefined" ? false : options.hub;

            if (window.document.testUrl !== 'auto' && !options.url.startsWith("http")) {
                options.url = window.document.testUrl + options.url;
            }

            connection = options.hub ?
                $.hubConnection(options.url, { useDefaultPath: false, qs: qs }) :
                $.connection(options.url, qs);
            connection.logging = true;

            if (!options.ignoreErrors) {
                connection.error(function (err) {
                    if (assert) {
                        assert.ok(false, "An error occurred during the connection: " + err.toString());
                    }
                    if (end) {
                        end();
                    }
                });
            }

            if (options.wrapStart) {
                wrapConnectionStart(connection, end, assert, options.ignoreErrors);
            }

            return connection;
        },
        createHubConnection: function (end, assert, testName, url, wrapStart) {
            return testUtilities.createTestConnection(testName, end, assert, { url: url, wrapStart: wrapStart, hub: true });
        },
        createConnection: function (url, end, assert, testName, wrapStart) {
            return testUtilities.createTestConnection(testName, end, assert, { url: url, wrapStart: wrapStart });
        }
    };

    // Create the transport names based off of the transport list
    testUtilities.transportNames = $.map(testUtilities.transports, function (_, transportName) {
        return transportName;
    });
})($, window);
