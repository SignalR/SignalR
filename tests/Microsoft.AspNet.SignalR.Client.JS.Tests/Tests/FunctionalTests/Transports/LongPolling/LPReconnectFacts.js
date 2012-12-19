module("Long Polling Facts");

QUnit.asyncTimeoutTest("Long Polling transport can reconnect.", 50000, function (end) {
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
                ok(true, "Reconnected");
                end();
            }
        });

        tryReconnect();
    };

    connection.start({ transport: 'longPolling' }).done(function () {
        ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    }).fail(function (reason) {
        ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});