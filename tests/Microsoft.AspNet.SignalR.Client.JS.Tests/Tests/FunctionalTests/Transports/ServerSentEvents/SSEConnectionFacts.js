module("Server Sent Events Facts");

QUnit.asyncTimeoutTest("Server Sent Events transport can connect.", 5000, function (end) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: 'serverSentEvents' }).done(function () {
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