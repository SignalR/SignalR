module("Long Polling Facts");

QUnit.asyncTimeoutTest("Long Polling transport can connect.", 5000, function (end) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: 'longPolling' }).done(function () {
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