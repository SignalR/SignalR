QUnit.module("Core - Negotiate Facts");

(function ($, window) {
    var negotiateTester = function (connection, transport, end, assert, testName) {
        var expectedQs = window.encodeURIComponent(testName),
            savedAjax = $.ajax;

        // Verify that the query string parameter was set
        assert.ok(connection.qs.indexOf(expectedQs) >= 0, "Query string contains the test name prior to connection start");

        $.ajax = function (url, settings) {
            if (!settings) {
                settings = url;
                url = settings.url;
            }

            // Check if it's the negotiate request;
            if (url.indexOf("/negotiate") >= 0) {
                // Verify that the query string parameter on the connection is passed via the ajax request
                assert.ok(url.indexOf(expectedQs) >= 0, "Query string parameters were passed in negotiate");
                // Let the ajax request finish out
                setTimeout(end, 0);
            }

            // Persist the request through to the original ajax request
            savedAjax.call(this, url, settings);
        };

        connection.start({ transport: transport });

        return savedAjax;
    };

    testUtilities.runWithAllTransports(function (transport) {
        QUnit.asyncTimeoutTest(transport + " transport negotiate passes query string variables for hub connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = negotiateTester(connection, transport, end, assert, testName);

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport negotiate passes query string variables for persistent connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", end, assert, testName),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = negotiateTester(connection, transport, end, assert, testName);

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport's negotiate triggers onNegotiated event.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", testName);

            connection.negotiated(function (settings) {
                assert.ok(true, "Negotiated event was called.");
                assert.isSet(settings, "Negotiate response settings object was passed.");
                assert.isSet(settings.ConnectionToken, "ConnectionToken exists on negotiate settings object.");
                assert.isSet(settings.Url, "Url exists on negotiate settings object.");
                assert.isSet(settings.ProtocolVersion, "ProtocolVersion exists on negotiate settings object.");
                assert.isSet(settings.DisconnectTimeout, "DisconnectTimeout exists on negotiate settings object.");
                assert.isSet(settings.TryWebSockets, "TryWebSockets exists on negotiate settings object.");
                assert.isSet(settings.KeepAliveTimeout, "KeepAliveTimeout exists on negotiate settings object.");

                end();
            });

            connection.start({ transport: transport }).fail(function () {
                assert.ok(false, "Failed to initiate SignalR connection.");
                end();
            });

            return function () {
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport's onNegotiated event allows modification of settings.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", testName);

            connection.negotiated(function (settings) {
                settings.ConnectionId = "Foo";
            });

            // Using the starting event because this occurs prior to any transports starting, therefore we can end execution.
            connection.starting(function () {
                assert.equal(connection.id, "Foo", "Connection ID was successfully modified to 'Foo'");
                end();
            });

            connection.start({ transport: transport }).fail(function () {
                assert.ok(false, "Failed to initiate SignalR connection.");
                end();
            });

            return function () {
                connection.stop();
            };
        });
    });
})($, window);