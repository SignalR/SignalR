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
    },
    testInvalidProtocol = function (connection, transport, end, assert, protocol) {
        connection.clientProtocol = protocol;

        connection.start({ transport: transport }).done(function () {
            assert.ok(false, "Transport started successfully.");
        }).fail(function () {
            assert.comment("Transport failed to start with incorrect protocol.");
            end();
        });
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

        QUnit.asyncTimeoutTest(transport + " transport negotiate passes client protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", end, assert, testName),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = $.ajax;

            $.ajax = function (url, settings) {
                if (!settings) {
                    settings = url;
                    url = settings.url;
                }

                // Check if it's the negotiate request;
                if (url.indexOf("/negotiate") >= 0) {
                    // Verify that the query string parameter on the connection is passed via the ajax request
                    assert.ok(url.indexOf("clientProtocol=" + connection.clientProtocol) >= 0, "Client protocol passed in negotiate request");
                    // Let the ajax request finish out
                    setTimeout(end, 0);
                }

                // Persist the request through to the original ajax request
                savedAjax.call(this, url, settings);
            };

            connection.start({ transport: transport });

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + ": connection fails to start with newer protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", end, assert, testName, false);

            testInvalidProtocol(connection, transport, end, assert, 1337);

            return function () {
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + ": connection fails to start with older protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createConnection("signalr", end, assert, testName, false);

            testInvalidProtocol(connection, transport, end, assert, .1337);

            return function () {
                connection.stop();
            };
        });

    });
})($, window);