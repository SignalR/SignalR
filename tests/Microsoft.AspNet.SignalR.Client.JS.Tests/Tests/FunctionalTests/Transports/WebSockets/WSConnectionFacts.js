QUnit.module("WebSockets Facts", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Can connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName);

    connection.start({ transport: 'webSockets' }).done(function () {
        assert.ok(true, "Connected");
        end();
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});