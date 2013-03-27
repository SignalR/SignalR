QUnit.module("Transports Common - Ajax Send Facts");

testUtilities.runWithTransports(["longPolling", "foreverFrame", "serverSentEvents"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can trigger method on server via ajaxSend.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo;

        // Must subscribe to at least one method on client
        demo.client.foo = function () { };

        connection.received(function (result) {
            if (result.I === "1") {
                if (result.R === 6) {
                    assert.ok(true, "Result successfully received from server via ajaxSend");
                }
                else {
                    assert.ok(false, "Invalid result returned from server via ajaxSend");
                }

                end();
            }
        });

        connection.start({ transport: transport }).done(function () {
            var data = {
                H: "demo",
                M: "Overload",
                A: [6],
                I: 1
            };

            $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    // This test verifies that #947 is fixed
    QUnit.asyncTimeoutTest(transport + " transport can trigger method on server via ajaxSend with userDefined ajax configurations.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            defaultContentType = $.ajaxSettings.contentType;

        $.ajaxSetup({ contentType: "application/json; charset=utf-8" });

        // Must subscribe to at least one method on client
        demo.client.foo = function () { };

        connection.received(function (result) {
            if (result.I === "1") {
                if (result.R === 6) {
                    assert.ok(true, "Result successfully received from server via ajaxSend");
                }
                else {
                    assert.ok(false, "Invalid result returned from server via ajaxSend");
                }

                end();
            }
        });

        connection.start({ transport: transport }).done(function () {
            var data = {
                H: "demo",
                M: "Overload",
                A: [6],
                I: 1
            };

            $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
        });

        // Cleanup
        return function () {
            $.ajaxSetup({ contentType: defaultContentType });
            connection.stop();
        };
    });
});

// WebSockets uses a duplex stream for sending content, thus does not use the ajax methods