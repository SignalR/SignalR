QUnit.module("Transports Common - Ajax Abort Facts");

testUtilities.runWithTransports(["longPolling", "foreverFrame", "serverSentEvents"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can trigger abort on server via ajaxAbort.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection1 = testUtilities.createHubConnection(end, assert, testName),
            connection2 = testUtilities.createHubConnection(end, assert, testName),
            statushub1 = connection1.createHubProxies().StatusHub,
            statushub2 = connection2.createHubProxies().StatusHub,
            transportInitializer = { transport: transport };

        // Need to register at least 1 callback in order to subscribe to hub.
        statushub1.client.foo = function () { };

        statushub2.client.leave = function () {
            assert.ok(true, "Ajax Abort (disconnect) successfully received on the server");
            end();
        };

        // Start both connections
        connection1.start(transportInitializer).done(function () {
            connection2.start(transportInitializer).done(function () {
                $.signalR.transports._logic.ajaxAbort(connection1);
            });
        });

        // Cleanup
        return function () {
            connection1.stop();
            connection2.stop();
        };
    });
});

// Web Sockets uses a duplex stream for sending content, thus does not use the ajax methods