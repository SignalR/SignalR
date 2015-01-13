var buildRedirectConnection = function (redirectWhen, end, assert, testName, wrapStart) {
    var connection = testUtilities.createConnection("redirectionConnection", end, assert, testName, wrapStart);

    connection.qs = {
        redirectWhen: redirectWhen
    };

    return connection;
};

QUnit.module("Response redirection facts");

QUnit.asyncTimeoutTest("Transport connect fails on response redirection with error message.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = buildRedirectConnection("negotiate", end, assert, testName, false);

    connection.error(function (error) {
        assert.isSet(error, "Error message shoud be set.");
        assert.notEqual(error, "", "Error message should not be empty string.");
    });

    connection.start().done(function () {
        assert.fail("Connection was started successfully.");
        end();
    }).fail(function () {
        assert.comment("Connection should fail.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Transport connect fails on response redirection causing negotiate to fallback through all transports.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = buildRedirectConnection("connect", end, assert, testName, false);

    connection.start().done(function () {
        assert.fail("Connection was started successfully.");
        end();
    }).fail(function () {
        assert.comment("Connection start deferred failure was triggered.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Auto transport negotiate fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = buildRedirectConnection("negotiate", end, assert, testName, false);

    connection.start().done(function () {
        assert.fail("Connection was started successfully.");
        end();
    }).fail(function (error) {
        assert.comment("Connection start deferred failure was triggered.");
        assert.isSet(error.context, "Context is passed with parse failure in negotiate.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + ": Negotiate fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = buildRedirectConnection("negotiate", end, assert, testName, false);

        connection.start({ transport: transport }).done(function () {
            assert.fail("Connection was started successfully.");
            end();
        }).fail(function (error) {
            assert.comment("Connection start deferred failure was triggered.");
            assert.isSet(error.context, "Context is passed with parse failure in negotiate.");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " - Ping server fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = buildRedirectConnection("ping", end, assert, testName, false),
            testPingServer = function () {
                $.signalR.transports._logic.pingServer(connection).done(function () {
                    // Successful ping
                    assert.fail("Successful ping with " + transport);
                    end();
                }).fail(function (error) {
                    assert.comment("Failed to ping server with " + transport);
                    assert.isSet(error.context, "Context is passed with parse failure in ping.");
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
});

testUtilities.runWithTransports(["longPolling", "foreverFrame", "serverSentEvents"], function (transport) {

    QUnit.asyncTimeoutTest(transport + ": Send fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = buildRedirectConnection("send", end, assert, testName, true);

        connection.start({ transport: transport }).done(function () {
            assert.comment("Connection was started successfully.");

            connection.send("");

            connection.error(function (error) {
                assert.comment("Connection triggered error function on bad send response.");
                assert.isSet(error.context, "Context is passed with parse failure in send.");
                end();
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

});

QUnit.asyncTimeoutTest("LongPolling poll fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = buildRedirectConnection("poll", end, assert, testName, false);

    connection.error(function (error) {
        assert.comment("Connection triggered error function on bad poll response.");
        // If the error occurs during the start request, it may be wrapped, so check the source too.
        assert.isSet(error.context || error.source.context, "Context is passed with parse failure in poll.");
        end();
    });

    connection.start({ transport: "longPolling" }).done(function () {
        // The connection will not start successfully if the error occurs during the start request.
        assert.comment("Connection was started successfully.");
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

