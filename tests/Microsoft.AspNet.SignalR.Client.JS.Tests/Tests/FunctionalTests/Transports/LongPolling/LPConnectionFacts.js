QUnit.module("Long Polling Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long Polling transport can connect.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection();

    connection.start({ transport: 'longPolling' }).done(function () {
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

QUnit.asyncTimeoutTest("Long Polling transport can receive messages on connect.", 5000, function (end, assert) {
    var connection = testUtilities.createConnection('multisend'),
        values = [];

    connection.received(function (data) {
        values.push(data);

        if (values.length === 2) {
            assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
            assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
            end();
        }
    });

    connection.start({ transport: 'longPolling' }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});