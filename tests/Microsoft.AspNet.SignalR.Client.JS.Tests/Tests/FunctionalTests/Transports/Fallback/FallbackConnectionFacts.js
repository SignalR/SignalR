QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start().done(function () {
        assert.ok(true, "Connected");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});