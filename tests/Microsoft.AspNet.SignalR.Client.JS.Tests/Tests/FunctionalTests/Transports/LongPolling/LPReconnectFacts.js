QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Can reconnect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Reconnecting");
                $.network.connect();
            } else if (connection.state === $.signalR.connectionState.connected) {
                assert.ok(true, "Reconnected");
                end();
            }
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Shifts into reconnecting state.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state.");
                end();
            }
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnecting.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnecting(function () {
            assert.ok(true, "Reconnecting triggered!");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnected.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        connection.reconnecting(function () {
            assert.ok(true, "Reconnecting triggered!");
            $.network.connect();
        });

        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnected(function () {
            assert.ok(true, "Reconnected triggered!");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Clears stop reconnecting timeout on stop inside of stateChanged.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo,
        // Trigger disconnect timeout after X second of trying to reconnect.  This has to be a unique value because
        // we'll be using it to check if we've triggered the disconnect timeout.
        disconnectTimeout = 1.337,
        timeoutId = -1;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state (via stateChanged), stopping the connection.");
                connection.stop();
                
                // Set a timeout for 3x the disconnectTimeout.  If it hasn't triggered by then we were successful.
                timeoutId = setTimeout(function () {
                    assert.ok(true, "Stop Reconnect timeout was not triggered, success!");
                    end();
                }, disconnectTimeout * 3);
            }
        });

        $.network.disconnect();
    };

    // Replace log function to see if we trigger reconnectTimeout.  Need to look at the log itself because
    // the rest of the logic is enclosed in a closure.
    connection.log = function (message) {
        // We triggered the disconnect timeout, bad!
        if (message.indexOf(disconnectTimeout) >= 0) {
            assert.ok(false, "Reconnecting Timed out.");
            end();
        }
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        connection.disconnectTimeout = disconnectTimeout;

        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        clearTimeout(timeoutId);
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Can remain connected to /signalr/hubs.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "signalr/hubs"),
        demo = connection.createHubProxies().demo,
        testGuidInvocations = 0;

    demo.client.TestGuid = function () {
        testGuidInvocations++;
        if (testGuidInvocations < 2) {
            assert.ok(true, "First testGuid invocation succeeded.");
            demo.server.testGuid();
        } else {
            assert.ok(true, "Second testGuid invocation succeeded.");
            end();
        }
    };

    connection.error(function (e) {
        assert.ok(false, "Connection error: " + e);
        end();
    });

    connection.reconnecting(function () {
        assert.ok(false, "Reconnecting should not be triggered");
        end();
    });

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

// For #1809
QUnit.asyncTimeoutTest("Does not reconnect infinitely if ping succeeds", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo,
        savedPingServer = $.signalR.transports._logic.pingServer,
        reconnectingTriggered = false;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        $.signalR.transports._logic.pingServer = function () {
            var deferral = $.Deferred();

            // Force the ping server to resolve in a timeout (act like an actual ajax request)
            window.setTimeout(function () {
                deferral.resolve();
            }, 100);

            return deferral.promise();
        };

        // Stop reconnecting after 6 seconds ( this overrides the negotiate value ).
        connection.disconnectTimeout = 6000;

        connection.reconnecting(function () {
            reconnectingTriggered = true;
        });

        connection.disconnected(function () {
            assert.ok(reconnectingTriggered, "Reconnecting was triggered within the test.");
            assert.ok(true, "Disconnect triggered, transport did not reconnect infinitely.");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        $.signalR.transports._logic.pingServer = savedPingServer;
        connection.stop();
        $.network.connect();
    };
});