QUnit.module("Transports Common - Ping Server Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long Polling transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        testPingServer = function () {
            $.signalR.transports._logic.pingServer(connection, "longPolling").done(function() {
                // Successful ping
                assert.ok(true, "Successful ping with Long Polling");
                end();
            }).fail(function () {
                assert.ok(false, "Failed to ping server with Long Polling");
                end();
            });            
        };

    // Starting/Stopping a connection to have it instantiated with all the appropriate variables
    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        connection.stop();
        testPingServer();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Forever Frame transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        testPingServer = function () {
            $.signalR.transports._logic.pingServer(connection, "foreverFrame").done(function () {
                // Successful ping
                assert.ok(true, "Successful ping with Forever Frame");
                end();
            }).fail(function () {
                assert.ok(false, "Failed to ping server with Forever Frame");
                end();
            });
        };

    // Starting/Stopping a connection to have it instantiated with all the appropriate variables
    connection.start({ transport: "foreverFrame" }).done(function () {
        assert.ok(true, "Connected");
        connection.stop();
        testPingServer();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        testPingServer = function () {
            $.signalR.transports._logic.pingServer(connection, "serverSentEvents").done(function () {
                // Successful ping
                assert.ok(true, "Successful ping with Server Sent Events");
                end();
            }).fail(function () {
                assert.ok(false, "Failed to ping server with Server Sent Events");
                end();
            });
        };

    // Starting/Stopping a connection to have it instantiated with all the appropriate variables
    connection.start({ transport: "serverSentEvents" }).done(function () {
        assert.ok(true, "Connected");
        connection.stop();
        testPingServer();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Web Sockets transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        testPingServer = function () {
            $.signalR.transports._logic.pingServer(connection, "webSockets").done(function () {
                // Successful ping
                assert.ok(true, "Successful ping with Web Sockets");
                end();
            }).fail(function () {
                assert.ok(false, "Failed to ping server with Web Sockets");
                end();
            });
        };

    // Starting/Stopping a connection to have it instantiated with all the appropriate variables
    connection.start({ transport: "webSockets" }).done(function () {
        assert.ok(true, "Connected");
        connection.stop();
        testPingServer();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});
