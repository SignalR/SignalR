QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start().done(function () {
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