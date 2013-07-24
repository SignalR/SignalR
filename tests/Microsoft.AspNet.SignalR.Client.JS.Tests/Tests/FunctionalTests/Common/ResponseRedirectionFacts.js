var buildRedirectConnection = function (redirectWhen, end, assert, testName, wrapStart) {
    var connection = testUtilities.createConnection("redirectionConnection", end, assert, testName, wrapStart);

    connection.qs = {
        redirectWhen: redirectWhen
    };

    return connection;
};

QUnit.module("Response redirection facts");

QUnit.asyncTimeoutTest("Transport connect fails on response redirection with error message.", testUtilities.defaultTestTimeout * 4000, function (end, assert, testName) {
    var connection = buildRedirectConnection("negotiate", end, assert, testName, false);

    connection.error(function (error) {
        assert.isSet(error, "Error message shoud be set.");
        assert.ok(error != "", "Error message should not be empty string.");
        end();
    });

    connection.start().done(function () {
        assert.fail("Connection was started successfully.");
        end();
    }).fail(function () {
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Transport connect fails on response redirection causing negotiate to fallback through all transports.", testUtilities.defaultTestTimeout * 4000, function (end, assert, testName) {
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
    }).fail(function () {
        assert.comment("Connection start deferred failure was triggered.");
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
        }).fail(function () {
            assert.comment("Connection start deferred failure was triggered.");
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
                $.signalR.transports._logic.pingServer(connection, transport).done(function () {
                    // Successful ping
                    assert.fail("Successful ping with " + transport);
                    end();
                }).fail(function () {
                    assert.comment("Failed to ping server with " + transport);
                    end();
                });
            };

        // Starting/Stopping a connection to have it instantiated with all the appropriate variables
        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            connection.stop();
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

            connection.error(function () {
                assert.comment("Connection triggered error function on bad send response.");
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
    var connection = buildRedirectConnection("poll", end, assert, testName, true);

    connection.start({ transport: "longPolling" }).done(function () {
        assert.comment("Connection was started successfully.");

        connection.send("");

        connection.error(function () {
            assert.comment("Connection triggered error function on bad poll response.");
            end();
        });
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

