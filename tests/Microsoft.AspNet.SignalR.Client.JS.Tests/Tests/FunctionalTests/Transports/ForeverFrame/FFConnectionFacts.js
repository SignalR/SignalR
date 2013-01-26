QUnit.module("Forever Frame Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Can connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName);

    connection.start({ transport: "foreverFrame" }).done(function () {
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