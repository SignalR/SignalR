QUnit.module("Server Sent Event Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Can connect.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: 'serverSentEvents' }).done(function () {
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