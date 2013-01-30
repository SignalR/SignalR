QUnit.module("Transports Common - Ajax Send Facts");

testUtilities.runWithTransports(["longPolling", "foreverFrame", "serverSentEvents"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can trigger method on server via ajaxSend.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName),
            demo = connection.createHubProxies().demo;

        // Must subscribe to at least one method on client
        demo.client.foo = function () { };

        connection.received(function (result) {
            if (result.R === 6) {
                assert.ok(true, "Result successfully received from server via ajaxSend");
            }
            else {
                assert.ok(false, "Invalid result returned from server via ajaxSend");
            }

            end();
        });

        connection.start({ transport: transport }).done(function () {
            var data = {
                H: "demo",
                M: "Overload",
                A: [6],
                I: 1
            };

            $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
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

// WebSockets uses a duplex stream for sending content, thus does not use the ajax methods