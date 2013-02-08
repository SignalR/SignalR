QUnit.module("Transports Common - Ping Server Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can initiate Ping Server.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName),
            testPingServer = function () {
                $.signalR.transports._logic.pingServer(connection, transport).done(function () {
                    // Successful ping
                    assert.ok(true, "Successful ping with " + transport);
                    end();
                }).fail(function () {
                    assert.ok(false, "Failed to ping server with " + transport);
                    end();
                });
            };

        // Starting/Stopping a connection to have it instantiated with all the appropriate variables
        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            connection.stop();
            testPingServer();
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate SignalR connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});