QUnit.module("Web Sockets Facts", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Can connect.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: 'webSockets' }).done(function () {
        assert.ok(true, "Connected");
        end();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});