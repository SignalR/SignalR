QUnit.module("Ping Server Facts - Long Polling", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long Polling transport can initiate Ping Server.", 5000, function (end, assert) {
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

QUnit.module("Ping Server Facts - Server Sent Events", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport can initiate Ping Server.", 5000, function (end, assert) {
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