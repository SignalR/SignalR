﻿module("Long Polling Facts");

QUnit.asyncTimeoutTest("Long Polling transport can reconnect.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
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

QUnit.asyncTimeoutTest("Long Polling transport shifts into reconnecting state.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
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

QUnit.asyncTimeoutTest("Long Polling transport triggers reconnecting.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
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

QUnit.asyncTimeoutTest("Long Polling transport triggers reconnected.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        tryReconnect = function () {
            if (connection.pollXhr.readyState !== 1) {
                setTimeout(tryReconnect, 200);
            }
            else {
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