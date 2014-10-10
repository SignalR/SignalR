QUnit.module("Hub Group Facts");

testUtilities.runWithAllTransports(function (transport) {
    // Reduce chance of conflicts with multiple simultaneous tests
    var randomGroup = window.Math.random().toString();

    QUnit.asyncTimeoutTest(transport + ": Hub can send and receive messages from groups.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            groupChat = connection.createHubProxies().groupChat;

        groupChat.client.send = function (value) {
            assert.ok(value === "hello", "Successfully received message from group");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");

            groupChat.server.join(randomGroup).done(function () {
                assert.ok(true, "Successful call to join group");

                groupChat.server.send(randomGroup, "hello");
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Rejoin Group after reconnect.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            groupChat = connection.createHubProxies().groupChat,
            readyToEnd = false;

        groupChat.client.send = function (value) {
            if (readyToEnd) {
                assert.ok(value === "hello", "Successful received message from group after reconnected");
                end();
            }
            else if (value === "TryToReconnect") {
                $.network.disconnect();
            }
        };

        connection.reconnecting(function () {
            assert.ok(true, "Connection is now attempting to reconnect");
            $.network.connect();
        });

        connection.reconnected(function () {
            assert.ok(true, "Successfuly raised reconnected event ");

            readyToEnd = true;
            groupChat.server.send(randomGroup, "hello").done(function () {
                assert.ok(true, "Successful send to group");
            });
        });

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");

            groupChat.server.join(randomGroup).done(function () {
                assert.ok(true, "Successful call to join group");

                // We must get a value back from the server in order to instantiate our message ID so longPolling is able to reconnect.
                groupChat.server.send(randomGroup, "TryToReconnect");
            });

        });

        // Cleanup
        return function () {
            connection.stop();
            $.network.connect();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport can join group in OnConnected and get message.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            groupJoiningHub = connection.createHubProxies().groupJoiningHub,
            pingCount = 0;

        groupJoiningHub.client.ping = function () {
            assert.comment("Ping received from group.");

            if (++pingCount === 2) {
                assert.comment("Pinged twice.");

                // Let sleep for 1 second to let any dups flow in (so we can fail)
                window.setTimeout(function () {
                    end();
                }, 1000);
            }

            assert.isTrue(pingCount <= 2, "Ping count is less than two (haven't received dups).");
        };

        connection.start({ transport: transport }).done(function () {
            groupJoiningHub.server.pingGroup();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});

