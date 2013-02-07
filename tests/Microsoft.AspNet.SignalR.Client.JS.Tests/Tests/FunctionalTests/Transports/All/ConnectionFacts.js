QUnit.module("Connection Facts");

testUtilities.runWithAllTransports(function (transport) {
   
    QUnit.asyncTimeoutTest(transport + " transport can send and receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 4) {
                assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
                assert.equal(values[2], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[3], "OnConnectedAsync2", "Received OnConnectedAsync2");
                end();
            }
        });

        connection.error(function (err) {
            assert.ok(false, "Error raised");
            end();
        });

        connection.start({ transport: transport }).done(function () {
            connection.send("test");
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate SignalR connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport can receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
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

        connection.start({ transport: transport }).fail(function (reason) {
            assert.ok(false, "Failed to initiate SignalR connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});