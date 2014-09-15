QUnit.module("Transports Common - Keep Alive Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Long polling transport does not check keep alive.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected.");
        assert.ok(!connection._.keepAliveData.monitoring, "We should not be monitoring the keep alive for the long polling transport.");
        end();
    });

    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Keep Alive Facts");

testUtilities.runWithTransports(["foreverFrame", "serverSentEvents", "webSockets"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport attempts to check keep alive.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected.");
            assert.ok(connection._.keepAliveData.monitoring === true, "We should be monitoring the keep alive for the " + transport + " transport.");

            end();
        });

        return function () {
            connection.stop();
        };
    });
});

QUnit.asyncTimeoutTest("Check if alive can recover from faulty connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        savedMarkLastMessage = $.signalR.transports._logic.markLastMessage,
        failed = true;

    // Null out the lastMessage function so our junk "markLastMessage" is used
    $.signalR.transports._logic.markLastMessage = function () { };

    connection.connectionSlow(function () {
        connection._.lastMessageAt = new Date().getTime(); // Set the lastMessageAt value to current date (we recovered!);
    });

    connection._.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        timeout: 100000
    };

    // Check every 100 milliseconds
    connection._.beatInterval = 100;

    // Set the time to be 3 seconds ago so we trigger a slow event first
    connection._.lastMessageAt = new Date(new Date().valueOf() - 3000).getTime();

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);
    $.signalR.transports._logic.startHeartbeat(connection);

    assert.ok(connection._.keepAliveData.userNotified === true, "User notified that they were slow (in faulty state).");

    connection._.lastMessageAt = new Date().getTime();

    // Wait 4x the beat interval, so it should have been registered as recovered by then (aka userNotified = false)
    setTimeout(function () {
        assert.equal(connection._.keepAliveData.userNotified, false, "CheckIfAlive recovers from faulty connection.");
        end();
    }, 4 * connection._.beatInterval);

    // Cleanup
    return function () {
        $.signalR.transports._logic.markLastMessage = savedMarkLastMessage;
        $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
        window.clearTimeout(connection._.beatHandle);
    };
});