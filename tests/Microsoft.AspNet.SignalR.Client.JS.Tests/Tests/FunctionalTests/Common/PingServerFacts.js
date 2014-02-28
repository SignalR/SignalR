QUnit.module("Transports Common - Ping Server Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            testPingServer = function () {
                $.signalR.transports._logic.pingServer(connection).done(function () {
                    // Successful ping
                    assert.ok(true, "Successful ping with " + transport);
                    end();
                }).fail(function () {
                    assert.ok(false, "Failed to ping server with " + transport);
                    end();
                });
            };

        // Starting/Stopping a connection to have it instantiated with all the appropriate variables
        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            testPingServer();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport calls Ping Server with custom query string in url", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            expectedQs = window.encodeURIComponent(testName),
            savedAjax = $.ajax,
            testPingServer = function () {
                $.signalR.transports._logic.pingServer(connection).done(function () {
                    // Successful ping
                    assert.ok(true, "Successful ping with " + transport);
                }).fail(function () {
                    assert.ok(false, "Failed to ping server with " + transport);
                    end();
                });
            };

        // Verify that the query string parameter was set
        assert.ok(connection.qs.indexOf(expectedQs) >= 0, "Query string contains the test name prior to ping server.");

        // For long polling this ajax request will execute before testPingServer because longPolling
        // utilizes the pingServer method.
        function ajaxReplacement(url, settings) {
            if (!settings) {
                settings = url;
                url = settings.url;
            }

            // Check if it's the ping request;
            if (url.indexOf("/ping") >= 0) {
                // Verify that the query string parameter on the connection is passed via the ajax request
                assert.ok(url.indexOf(expectedQs) >= 0, "Query string parameters were passed in ping server");
                // Let the ajax request finish out
                setTimeout(end, 0);
            }

            // Persist the request through to the original ajax request
            return savedAjax.call(this, url, settings);
        };

        // Starting/Stopping a connection to have it instantiated with all the appropriate variables
        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            $.ajax = ajaxReplacement;
            testPingServer();
        });

        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });
});