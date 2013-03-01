QUnit.module("Transports Common - Keep Alive Facts");

QUnit.test("Monitor keep alive should initialize the lastKeepAlive flag.", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };

    $.signalR.transports._logic.monitorKeepAlive(connection);
    QUnit.ok(connection.keepAliveData.lastKeepAlive !== false, "Last keep alive should be set on the initialization of monitoring.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Only starts monitoring keep alive if not already monitoring.", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };

    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if we try and monitor it again
    connection.keepAliveData.lastKeepAlive = false;

    $.signalR.transports._logic.monitorKeepAlive(connection);

    QUnit.ok(connection.keepAliveData.lastKeepAlive === false, "Last keep alive should still be set to false because we should not have attempted to re-monitor the connection.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Save updateKeepAlive binding so it can be unbound later.", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };

    QUnit.ok(!connection.keepAliveData.reconnectKeepAliveUpdate, "Binding does not exist prior to monitor");
    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if our saved updateKeepAlive binding is saved
    connection.keepAliveData.lastKeepAlive = false

    QUnit.ok(connection.keepAliveData.reconnectKeepAliveUpdate, "Binding exists after monitor");

    connection.keepAliveData.reconnectKeepAliveUpdate();
    QUnit.ok(connection.keepAliveData.lastKeepAlive !== false, "Last keep alive should have changed due to the bound function executing.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("UpdateKeepAlive binding is triggered on reconnect", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };
    
    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if our saved updateKeepAlive binding is executed on reconnect
    connection.keepAliveData.lastKeepAlive = false;

    $(connection).triggerHandler($.signalR.events.onReconnect);

    QUnit.ok(connection.keepAliveData.lastKeepAlive !== false, "Last keep alive should have changed due to the bound function executing from onReconnect.");

    // Cleanup
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});

QUnit.test("Stop monitoring handles monitoring flag appropriately.", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };

    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
    QUnit.isNotSet(connection.keepAliveData.monitoring, "The keep alive monitoring should still be unset, meaning stop did not trigger.");

    // Start monitoring so we can stop
    $.signalR.transports._logic.monitorKeepAlive(connection);
    QUnit.ok(connection.keepAliveData.monitoring === true, "Keep alive monitoring flag set to true after monitorKeepAlive.");

    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
    QUnit.isNotSet(connection.keepAliveData.monitoring, "Does not noop if monitor flag was enabled.");
});

QUnit.test("Stop monitoring unbinds reconnect flag.", function () {
    var connection = testUtilities.createHubConnection();

    // Ensure the connection can utilize the keep alive features
    connection.keepAliveData = {
        lastKeepAlive: false
    };

    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Reset the last keep alive to false so we can determine if our saved updateKeepAlive binding is executed on reconnect
    connection.keepAliveData.lastKeepAlive = false;

    $(connection).triggerHandler($.signalR.events.onReconnect);

    QUnit.ok(connection.keepAliveData.lastKeepAlive !== false, "Last keep alive should have changed due to the bound function executing from onReconnect.");

    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);

    QUnit.isNotSet(connection.keepAliveData.lastKeepAlive, "Last keep alive gets unset on stop monitoring of keep alive.");

    $(connection).triggerHandler($.signalR.events.onReconnect);
    QUnit.isNotSet(connection.keepAliveData.lastKeepAlive, "Last keep alive is still unset after triggering the reconnect event because it was unbound in stop monitoring.");
});

QUnit.test("Check if alive triggers OnConnectionSlow when keep out warning threshold is surpassed.", function () {
    var connection = testUtilities.createHubConnection(),
        savedUpdateKeepAlive = $.signalR.transports._logic.updateKeepAlive,
        failed = true;

    connection.connectionSlow(function () {
        failed = false;
    });

    // Null out the update Keep alive function so our junk "lastKeepAlive" is used
    $.signalR.transports._logic.updateKeepAlive = function () { };

    connection.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        lastKeepAlive: new Date(new Date().valueOf() - 3000), // Set the last keep alive to 3 seconds ago
        timeout: 100000, // Large value so we don't timeout when we're looking for slow
        userNotified: false
    };

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);

    QUnit.ok(!failed, "ConnectionSlow triggered on checkIfAlive (via monitorKeepAlive) if we breach the warn threshold.");    

    // Cleanup
    $.signalR.transports._logic.updateKeepAlive = savedUpdateKeepAlive;
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);    
});

QUnit.test("Check if alive detects transport timeout when keep out warning threshold is surpassed.", function () {
    var connection = testUtilities.createHubConnection(),
        savedUpdateKeepAlive = $.signalR.transports._logic.updateKeepAlive,
        keepAliveTimeout = 10000,
        failed = true;

    connection.connectionSlow(function () {
        failed = false;
    });

    // Null out the update Keep alive function so our junk "lastKeepAlive" is used
    $.signalR.transports._logic.updateKeepAlive = function () { };

    connection.keepAliveData = {
        timeoutWarning: 1000, // We should warn if the time difference between now and the last keep alive is greater than 1 second
        lastKeepAlive: new Date(new Date().valueOf() - 2 * keepAliveTimeout), // Set the last keep alive to 2x the timeout to ensure we timeout
        timeout: keepAliveTimeout, 
        userNotified: false
    };

    connection.transport = {};
    connection.transport.lostConnection = function () {
        failed = false;
    };

    connection.state = $.signalR.connectionState.connected;

    // Start monitoring keep alive again
    $.signalR.transports._logic.monitorKeepAlive(connection);
    // Set the last keep alive to a value that should trigger a timeout    

    QUnit.ok(!failed, "Lost Connection called on checkIfAlive (via monitorKeepAlive) if we breach the timeout threshold.");

    // Cleanup
    $.signalR.transports._logic.updateKeepAlive = savedUpdateKeepAlive;
    $.signalR.transports._logic.stopMonitoringKeepAlive(connection);
});