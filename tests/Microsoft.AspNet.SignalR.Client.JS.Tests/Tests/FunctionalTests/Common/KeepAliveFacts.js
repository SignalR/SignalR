QUnit.module("Transports Common - Keep Alive Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long polling transport does not check keep alive.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected.");
        assert.ok(!connection.keepAliveData.monitoring, "We should not be monitoring the keep alive for the long polling transport.");
        end();
    });

    return function () {
        connection.stop();
    }
});

QUnit.module("Transports Common - Keep Alive Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Forever Frame transport attempts to check keep alive.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: "foreverFrame" }).done(function () {
        assert.ok(true, "Connected.");
        assert.ok(connection.keepAliveData.monitoring === true, "We should be monitoring the keep alive for the forever frame transport.");
        end();
    });

    return function () {
        connection.stop();
    }
});

QUnit.module("Transports Common - Keep Alive Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport attempts to check keep alive.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: "serverSentEvents" }).done(function () {
        assert.ok(true, "Connected.");
        assert.ok(connection.keepAliveData.monitoring === true, "We should be monitoring the keep alive for the server sent events transport.");
        end();
    });

    return function () {
        connection.stop();
    }
});

QUnit.module("Transports Common - Keep Alive Facts", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Web Sockets transport attempts to check keep alive.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: "webSockets" }).done(function () {
        assert.ok(true, "Connected.");
        assert.ok(connection.keepAliveData.monitoring === true, "We should be monitoring the keep alive for the web sockets transport.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    }
});

QUnit.asyncTimeoutTest("Check if alive can recover from faulty connections.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        savedUpdateKeepAlive = $.signalR.transports._logic.updateKeepAlive,
        failed = true;

    // Null out the update Keep alive function so our junk "lastKeepAlive" is used
    $.signalR.transports._logic.updateKeepAlive = function () { };

    connection.connectionSlow(function () {
        connection.keepAliveData.lastKeepAlive = new Date(); // Set the lastKeepAlive to current date (we recovered!);
    });    

    connection.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        lastKeepAlive: new Date(new Date().valueOf() - 3000), // Set the time to be 3 seconds ago so we trigger a slow event first
        timeout: 100000,
        checkInterval: 100 // Check every 100 milliseconds
    };

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);

    assert.ok(connection.keepAliveData.userNotified === true, "User notified that they were slow (in faulty state).");

    // Turn off monitoring so checkIfAlive is not checked more than once
    connection.keepAliveData.monitoring = false;

    // Wait 4x the check interval, so it should have been registered as recovered by then (aka userNotified = false)
    setTimeout(function () {
        assert.equal(connection.keepAliveData.userNotified, false, "CheckIfAlive recovers from faulty connection.");
        end();
    }, 4 * connection.keepAliveData.checkInterval);

    // Cleanup
    return function () {
        $.signalR.transports._logic.updateKeepAlive = savedUpdateKeepAlive;
        $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
    }
});