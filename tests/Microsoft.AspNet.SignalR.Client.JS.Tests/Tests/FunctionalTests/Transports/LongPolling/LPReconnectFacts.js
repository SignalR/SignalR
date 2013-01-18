QUnit.module("Long Polling Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Can reconnect.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            // Verify that the polling connection is instantiated before trying to abort it. We want
            // to cause the transport to error so it must be instantiated first.
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
                // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                // forcing the transport into reconnecting.
                connection.pollXhr.abort("foo");
            }
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.connected) {
                assert.ok(true, "Reconnected");
                end();
            }
        });

        tryReconnect();
    };

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Shifts into reconnecting state.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            // Verify that the polling connection is instantiated before trying to abort it. We want
            // to cause the transport to error so it must be instantiated first.
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
                // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                // forcing the transport into reconnecting.
                connection.pollXhr.abort("foo");
            }
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state.");
                end();
            }
        });

        tryReconnect();
    };

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnecting.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            // Verify that the polling connection is instantiated before trying to abort it. We want
            // to cause the transport to error so it must be instantiated first.
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
                // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                // forcing the transport into reconnecting.
                connection.pollXhr.abort("foo");
            }
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnecting(function () {
            assert.ok(true, "Reconnecting triggered!");
            end();
        });

        tryReconnect();
    };

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnected.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            // Verify that the polling connection is instantiated before trying to abort it. We want
            // to cause the transport to error so it must be instantiated first.
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
                // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                // forcing the transport into reconnecting.
                connection.pollXhr.abort("foo");
            }
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnected(function () {
            assert.ok(true, "Reconnected triggered!");
            end();
        });

        tryReconnect();
    };

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Clears stop reconnecting timeout on stop inside of stateChanged.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        // Trigger disconnect timeout after X second of trying to reconnect.  This has to be a unique value because
        // we'll be using it to check if we've triggered the disconnect timeout.
        disconnectTimeout = 1.337,
        timeoutId = -1,
        tryReconnect = function () {
            // Verify that the polling connection is instantiated before trying to abort it. We want
            // to cause the transport to error so it must be instantiated first.
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
                // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                // forcing the transport into reconnecting.
                connection.pollXhr.abort("foo");
            }
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state (via stateChanged), stopping the connection.");
                connection.stop();
                
                // Set a timeout for 3x the disconnectTimeout.  If it hasn't triggered by then we were successful.
                timeoutId = setTimeout(function () {
                    assert.ok(true, "Stop Reconnect timeout was not triggered, success!");
                    end();
                }, disconnectTimeout * 3);
            }
        });

        tryReconnect();
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

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.ok(true, "Connected");
        connection.disconnectTimeout = disconnectTimeout;

        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        clearTimeout(timeoutId);
        connection.stop();
    };
});