QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName);

    connection.start().done(function () {
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