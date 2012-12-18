module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", 5000, function (end) {
    var connection = testUtilities.createHubConnection();

    connection.start().done(function () {
        ok(true, "Connected");
        end();
    }).fail(function (reason) {
        ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});