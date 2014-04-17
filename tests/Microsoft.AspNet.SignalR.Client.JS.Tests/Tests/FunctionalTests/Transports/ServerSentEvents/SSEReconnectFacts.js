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
        window.EventSource = savedEventSource;
        $.signalR.transports.serverSentEvents.reconnect = savedReconnect;
        $.network.connect();
        connection.stop();
    };
});


QUnit.asyncTimeoutTest("Clears reconnectAttemptTimeout on stop", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName);

    connection.reconnectDelay = 0;

    connection.reconnecting(function () {
        assert.comment("Started reconnecting");

        // Wait for $.signalR.transports.serverSentEvents.start to finish
        window.setTimeout(function () {
            assert.isSet(connection._.reconnectAttemptTimeoutHandle, "The reconnectAttemptTimeoutHandle is set.");
            connection.stop();
            assert.isNotSet(connection._.reconnectAttemptTimeoutHandle, "The reconnectAttemptTimeoutHandle has been cleared on connection stop.");
            end();
        }, 0);
    });

    connection.reconnected(function () {
        assert.fail("Unexpected successful reconnection");
    });

    connection.start({ transport: "serverSentEvents" }).done(function () {
        $.network.disconnect();
    });

    // Cleanup
    return function () {
        $.network.connect();
        connection.stop();
    };
});