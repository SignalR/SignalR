QUnit.module("Server Sent Events Facts", testUtilities.transports.serverSentEvents.enabled);

QUnit.asyncTimeoutTest("Attempts to reconnect at the correct interval.", testUtilities.defaultTestTimeout* 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName),
        savedEventSource = window.EventSource,
        savedReconnect = $.signalR.transports.serverSentEvents.reconnect;

    connection.start({ transport: "serverSentEvents" }).done(function () {
        var reconnectCount = 0;

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
        $.signalR.transports.serverSentEvents.reconnect = savedReconnect;
        $.network.connect();
    };
});


QUnit.asyncTimeoutTest("Clears reconnectAttemptTimeout on stop", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName),
        savedStart = $.signalR.transports.serverSentEvents.start;

    connection.reconnectDelay = 0;
    connection.start({ transport: "serverSentEvents" }).done(function () {
        connection.transport.start = function () {
            assert.comment("Successfully started reconnecting");
            savedStart.apply(this, arguments);

            assert.comment("Stopping connection")
            connection.stop();

            // Give time for the reconnectAttemptTimeout to fire
            window.setTimeout(end, 5000);
        };

        connection.reconnected(function () {
            assert.fail("Unexpected successful reconnection");
        })

        $.network.disconnect();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.signalR.transports.serverSentEvents.start = savedStart;
        $.network.connect();
    };
});