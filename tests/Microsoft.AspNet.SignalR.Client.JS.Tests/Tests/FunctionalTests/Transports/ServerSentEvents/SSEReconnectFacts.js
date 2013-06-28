QUnit.module("Server Sent Events Facts", testUtilities.transports.serverSentEvents.enabled);

QUnit.asyncTimeoutTest("Attempts to reconnect at the correct interval.", testUtilities.defaultTestTimeout* 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName);

    connection.start({ transport: "serverSentEvents" }).done(function () {
        var savedReconnect = connection.transport.reconnect,
            reconnectCount = 0;

        assert.ok(true, "Connected");

        connection.transport.reconnect = function () {
            reconnectCount++;

            assert.comment("Reconnecting");
            if (reconnectCount >= 3) {
                assert.comment("Reconnect occurred 3 times.");
                end();
            }

            savedReconnect.apply(this, arguments);
        };

        $.network.disconnect();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});