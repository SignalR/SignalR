QUnit.module("Server Sent Events Facts", testUtilities.transports.serverSentEvents.enabled);

QUnit.asyncTimeoutTest("Attempts to reconnect at the correct interval.", testUtilities.defaultTestTimeout* 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName),
        savedEventSource = window.EventSource;

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

        function CustomEventSource() {
            this.readyState = savedEventSource.CONNECTING;            

            return savedEventSource.apply(this, arguments);
        }

        $.extend(CustomEventSource, savedEventSource);

        window.EventSource = CustomEventSource;

        $.network.disconnect();
    });

    // Cleanup
    return function () {
        connection.stop();
        window.EventSource = savedEventSource;
        $.network.connect();
    };
});