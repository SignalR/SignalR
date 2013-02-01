﻿QUnit.module("Transports Common - Ping Server Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Long Polling transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
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
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.transports.foreverFrame.enabled);

QUnit.asyncTimeoutTest("Forever Frame transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
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
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.transports.serverSentEvents.enabled);

QUnit.asyncTimeoutTest("Server Sent Events transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
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
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ping Server Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSockets transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        testPingServer = function () {
            $.signalR.transports._logic.pingServer(connection, "webSockets").done(function () {
                // Successful ping
                assert.ok(true, "Successful ping with WebSockets");
                end();
            }).fail(function () {
                assert.ok(false, "Failed to ping server with WebSockets");
                end();
            });
        };

    // Starting/Stopping a connection to have it instantiated with all the appropriate variables
    connection.start({ transport: "webSockets" }).done(function () {
        assert.ok(true, "Connected");
        connection.stop();
        testPingServer();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});
