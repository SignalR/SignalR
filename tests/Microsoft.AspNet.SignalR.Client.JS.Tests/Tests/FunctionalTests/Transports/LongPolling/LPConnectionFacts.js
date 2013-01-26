QUnit.module("Long Polling Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Can connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName);

    connection.start({ transport: "longPolling" }).done(function () {
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

QUnit.asyncTimeoutTest("Can receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection("multisend", testName),
        values = [];

    connection.received(function (data) {
        values.push(data);

        if (values.length === 2) {
            assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
            assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
            end();
        }
    });

    connection.start({ transport: "longPolling" }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});