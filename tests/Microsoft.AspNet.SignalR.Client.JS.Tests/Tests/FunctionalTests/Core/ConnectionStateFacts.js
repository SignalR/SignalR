QUnit.module("Connection State Facts - Long Polling", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Connection shifts into appropriate states - Long Polling", 10000, function (end, assert) {
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
    demo.client.foo = function () { };

    assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to start.");

    connection.start({ transport: 'longPolling' }).done(function () {
        assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

        // Wire up the state changed (while connected) to detect if we shift into reconnecting
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "SignalR state is reconnecting.");
                end();
            }
        });

        tryReconnect();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");
    
    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Connection State Facts - Forever Frame", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Connection shifts into appropriate states - Forever Frame", 10000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            connection.transport.lostConnection(connection);
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.foo = function () { };

    assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to start.");

    connection.start({ transport: 'foreverFrame' }).done(function () {
        assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

        // Wire up the state changed (while connected) to detect if we shift into reconnecting
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "SignalR state is reconnecting.");
                end();
            }
        });

        tryReconnect();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Connection State Facts - Server Sent Events", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Connection shifts into appropriate states - Server Sent Events", 10000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            connection.transport.lostConnection(connection);
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.foo = function () { };

    assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to start.");

    connection.start({ transport: 'serverSentEvents' }).done(function () {
        assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

        // Wire up the state changed (while connected) to detect if we shift into reconnecting
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "SignalR state is reconnecting.");
                end();
            }
        });

        tryReconnect();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Connection State Facts - Web Sockets", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Connection shifts into appropriate states - Web Sockets", 10000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            connection.transport.lostConnection(connection);
        };

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.foo = function () { };

    assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to start.");

    connection.start({ transport: 'webSockets' }).done(function () {
        assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

        // Wire up the state changed (while connected) to detect if we shift into reconnecting
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state == $.signalR.connectionState.reconnecting) {
                assert.ok(true, "SignalR state is reconnecting.");
                end();
            }
        });

        tryReconnect();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");

    // Cleanup
    return function () {
        connection.stop();
    };
});