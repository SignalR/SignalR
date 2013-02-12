QUnit.module("Hub Group Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + ": Hub can send and receive messages from groups.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName),
            groupChat = connection.createHubProxies().groupChat;

        groupChat.client.send = function (value) {
            assert.ok(value === "hello", "Successfully received message from group");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");

            groupChat.server.join("group++1").done(function () {
                assert.ok(true, "Successful call to join group");

                groupChat.server.send("group++1", "hello");
            });
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate SignalR connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Rejoin Group after reconnect.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName),
            groupChat = connection.createHubProxies().groupChat,
            readyToEnd = false,
            tryReconnect;

        // LongPolling does not support "lostConnection" so we have to trigger reconnect in a different fashion
        if (transport !== "longPolling") {
            tryReconnect = function () {
                connection.transport.lostConnection(connection);
            };
        }
        else {
            tryReconnect = function () {
                // Verify that the polling connection is instantiated before trying to abort it. We want
                // to cause the transport to error so it must be instantiated first.
                if (connection.pollXhr.readyState !== 1) {
                    setTimeout(tryReconnect, 200);
                }
                else {
                    // Passing "foo" forces the longPolling's ajax connection to error and pass "foo" as the 
                    // reason, the default error (empty) is "abort" which we handle as do not attempt to 
                    // reconnect. So by passing foo we mimic the behavior of an unintended error occurring, 
                    // forcing the transport into reconnecting.
                    connection.pollXhr.abort("foo");
                }
            };
        }

        groupChat.client.send = function (value) {
            if (readyToEnd) {
                assert.ok(value === "hello", "Successful received message from group after reconnected");
                end();
            }
        };

        connection.reconnected(function () {
            assert.ok(true, "Successfuly raised reconnected event ");

            readyToEnd = true;
            groupChat.server.send("group++1", "hello").done(function () {
                assert.ok(true, "Successful send to group");
            });
        });

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");

            groupChat.server.join("group++1").done(function () {
                assert.ok(true, "Successful call to join group");

                // We must get a value back from the server in order to instantiate our message ID so longPolling is able to reconnect.
                groupChat.server.send("group++1", "foo").done(function () {
                    setTimeout(function () {
                        tryReconnect();
                    }, 200);
                });
            });

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

