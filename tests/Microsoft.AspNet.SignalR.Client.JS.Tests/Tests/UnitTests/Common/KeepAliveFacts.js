QUnit.module("Transports Common - Keep Alive Facts");

QUnit.test("Only starts monitoring keep alive if not already monitoring.", function () {
    var connection = testUtilities.createHubConnection();

    $.signalR.transports._logic.monitorKeepAlive(connection);
    connection._.keepAliveData.monitoring = 1;

    $.signalR.transports._logic.monitorKeepAlive(connection);

    QUnit.ok(connection._.keepAliveData.monitoring === 1, "Monitoring should still be set to 1 because we should not have attempted to re-monitor the connection (otherwise it'd be set to true).");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Save updateKeepAlive binding so it can be unbound later.", function () {
    var connection = testUtilities.createHubConnection();

    QUnit.ok(!connection._.keepAliveData.reconnectKeepAliveUpdate, "Binding does not exist prior to monitor");
    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if our saved updateKeepAlive binding is saved
    connection._.lastMessageAt = false;

    QUnit.ok(connection._.keepAliveData.reconnectKeepAliveUpdate, "Binding exists after monitor");

    connection._.keepAliveData.reconnectKeepAliveUpdate();
    QUnit.ok(connection._.lastMessageAt !== false, "Last message time stamp should have changed due to the bound function executing.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("UpdateKeepAlive binding is triggered on reconnect", function () {
    var connection = testUtilities.createHubConnection();

    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if our saved updateKeepAlive binding is executed on reconnect
    connection._.lastMessageAt = false;

    $(connection).triggerHandler($.signalR.events.onReconnect);

    QUnit.ok(connection._.lastMessageAt !== false, "Last message time stamp should have changed due to the bound function executing from onReconnect.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Stop monitoring handles monitoring flag appropriately.", function () {
    var connection = testUtilities.createHubConnection();

    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
    QUnit.isNotSet(connection._.keepAliveData.monitoring, "The keep alive monitoring should still be unset, meaning stop did not trigger.");

    // Start monitoring so we can stop
    $.signalR.transports._logic.monitorKeepAlive(connection);
    QUnit.ok(connection._.keepAliveData.monitoring === true, "Keep alive monitoring flag set to true after monitorKeepAlive.");

    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
    QUnit.isNotSet(connection._.keepAliveData.monitoring, "Does not noop if monitor flag was enabled.");
});

QUnit.test("Check if alive triggers OnConnectionSlow when keep out warning threshold is surpassed.", function () {
    var connection = testUtilities.createHubConnection(),
        savedMarkLastMessage = $.signalR.transports._logic.markLastMessage,
        failed = true;

    connection.connectionSlow(function () {
        failed = false;
    });

    // Null out the lastMessage function so our junk "markLastMessage" is used
    $.signalR.transports._logic.markLastMessage = function () { };

    connection._.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        timeout: 100000, // Large value so we don't timeout when we're looking for slow
        userNotified: false
    };

    connection._.lastMessageAt = new Date(new Date().valueOf() - 3000).getTime(); // Set the last message to 3 seconds ago

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);
    $.signalR.transports._logic.startHeartbeat(connection);

    QUnit.ok(!failed, "ConnectionSlow triggered on checkIfAlive (via monitorKeepAlive) if we breach the warn threshold.");

    // Cleanup
    $.signalR.transports._logic.markLastMessage = savedMarkLastMessage;
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Check if alive detects transport timeout when keep out warning threshold is surpassed.", function () {
    var connection = testUtilities.createHubConnection(),
        savedMarkLastMessage = $.signalR.transports._logic.markLastMessage,
        keepAliveTimeout = 10000,
        failed = true;

    connection.connectionSlow(function () {
        failed = false;
    });

    // Null out the lastMessage function so our junk "markLastMessage" is used
    $.signalR.transports._logic.markLastMessage = function () { };

    connection._.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        timeout: keepAliveTimeout,
        userNotified: false
    };

    connection._.lastMessageAt = new Date(new Date().valueOf() - 2 * keepAliveTimeout).getTime(); // Set the last keep alive to 2x the timeout to ensure we timeout

    connection.transport = {};
    connection.transport.lostConnection = function () {
        failed = false;
    };

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);
    $.signalR.transports._logic.startHeartbeat(connection);
    // Set the last keep alive to a value that should trigger a timeout    

    QUnit.ok(!failed, "Lost Connection called on checkIfAlive (via monitorKeepAlive) if we breach the timeout threshold.");

    // Cleanup
    $.signalR.transports._logic.markLastMessage = savedMarkLastMessage;

    connection.transport = null;

    connection.stop();
});