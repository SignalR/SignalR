QUnit.module("Transports Common - Ajax Abort Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long Polling transport can trigger abort on server via ajaxAbort.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection1 = testUtilities.createHubConnection(),
        connection2 = testUtilities.createHubConnection(),
        statushub1 = connection1.createHubProxies().StatusHub,
        statushub2 = connection2.createHubProxies().StatusHub,
        transport = { transport: "longPolling" };

    // Need to register at least 1 callback in order to subscribe to hub.
    statushub1.client.foo = function () { };

    statushub2.client.leave = function () {
        assert.ok(true, "Ajax Abort (disconnect) successfully received on the server");
        end();
    };

    // Start both connections
    connection1.start(transport).done(function () {
        connection2.start(transport).done(function () {
            $.signalR.transports._logic.ajaxAbort(connection1);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection2");
            end();
        });
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection1");
        end();
    });

    // Cleanup
    return function () {
        connection1.stop();
        connection2.stop();
    };
});

QUnit.module("Transports Common - Ajax Abort Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Forever Frame transport can trigger abort on server via ajaxAbort.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection1 = testUtilities.createHubConnection(),
        connection2 = testUtilities.createHubConnection(),
        statushub1 = connection1.createHubProxies().StatusHub,
        statushub2 = connection2.createHubProxies().StatusHub,
        transport = { transport: "foreverFrame" };

    // Need to register at least 1 callback in order to subscribe to hub.
    statushub1.client.foo = function () { };

    statushub2.client.leave = function () {
        assert.ok(true, "Ajax Abort (disconnect) successfully received on the server");
        end();
    };

    // Start both connections
    connection1.start(transport).done(function () {
        connection2.start(transport).done(function () {
            $.signalR.transports._logic.ajaxAbort(connection1);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection2");
            end();
        });
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection1");
        end();
    });

    // Cleanup
    return function () {
        connection1.stop();
        connection2.stop();
    };
});

QUnit.module("Transports Common - Ajax Abort Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport can trigger abort on server via ajaxAbort.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection1 = testUtilities.createHubConnection(),
        connection2 = testUtilities.createHubConnection(),
        statushub1 = connection1.createHubProxies().StatusHub,
        statushub2 = connection2.createHubProxies().StatusHub,
        transport = { transport: "serverSentEvents" };

    // Need to register at least 1 callback in order to subscribe to hub.
    statushub1.client.foo = function () { };

    statushub2.client.leave = function () {
        assert.ok(true, "Ajax Abort (disconnect) successfully received on the server");
        end();
    };

    // Start both connections
    connection1.start(transport).done(function () {
        connection2.start(transport).done(function () {
            $.signalR.transports._logic.ajaxAbort(connection1);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection2");
            end();
        });
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection1");
        end();
    });

    // Cleanup
    return function () {
        connection1.stop();
        connection2.stop();
    };
});

// Web Sockets uses a duplex stream for sending content, thus does not use the ajax methods