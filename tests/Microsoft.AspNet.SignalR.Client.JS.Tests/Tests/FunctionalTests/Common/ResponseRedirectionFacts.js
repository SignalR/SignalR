var buildRedirectConnection = function (redirectWhen, end, assert, testName, wrapStart) {
    var connection = testUtilities.createConnection("redirectionConnection", end, assert, testName, wrapStart);

    connection.qs = {
        redirectWhen: redirectWhen
    };

    return connection;
};

QUnit.module("Response redirection facts");

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

    // Canont do this for long polling because it utilizes the pingServer in its connect process
    if (transport !== "longPolling") {
        QUnit.asyncTimeoutTest(transport + " - Ping server fails on response redirection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = buildRedirectConnection("ping", end, assert, testName),
                testPingServer = function () {
                    $.signalR.transports._logic.pingServer(connection).done(function () {
                        // Successful ping
                        assert.fail("Successful ping with " + transport);
                        end();
                    }).fail(function (error) {
                        assert.comment("Failed to ping server with " + transport);
                        end();
                    });
                };

            // Starting/Stopping a connection to have it instantiated with all the appropriate variables
            connection.start({ transport: transport }).done(function () {
                assert.comment("Connected");
                testPingServer();
            });

            // Cleanup
            return function () {
                connection.stop();
            };
        });
    }
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
    var connection = buildRedirectConnection("poll", end, assert, testName);

    connection.start({ transport: "longPolling" }).done(function () {
        assert.comment("Connection was started successfully.");

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

