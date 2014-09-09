QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Messages received immediately after connectivity re-establishment triggers the reconnected event.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        transport = { transport: "longPolling" },
        demo = connection.createHubProxies().demo,
        reconnectingTriggered = false;

    demo.client.testGuid = function () { };

    connection.reconnecting(function () {
        reconnectingTriggered = true;
    });

    connection.reconnected(function () {
        assert.isTrue(reconnectingTriggered, "Reconnecting triggered.");
        assert.comment("Reconnected triggered.");
        end();
    });

    connection.start(transport).done(function () {
        assert.comment("Connected, turning off/on network.");

        // Wait for next poll to be established.
        setTimeout(function () {
            $.network.disconnect();
            $.network.connect();

            demo.server.testGuid();
        }, 1000);
    });

    // Cleanup
    return function () {
        $.network.connect();
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Connection start mid-reconnect does not cause double connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        transport = { transport: "longPolling" },
        savedAjax = $.ajax;

    connection.start(transport).done(function () {
        assert.comment("Connected");

        $.network.disconnect();

        setTimeout(function () {
            $.network.connect();

            connection.stop();
            connection.start(transport).done(function () {
                setTimeout(function () {
                    $.ajax = function () {
                        // Let ajax request finish
                        setTimeout(function () {
                            assert.fail("Ajax called when we weren't expecting.");
                            end();
                        }, 0);

                        // Persist the request through to the original ajax request
                        return savedAjax.apply(this, arguments);
                    };

                    setTimeout(function () {
                        assert.comment("No ajax requests were triggered.");
                        end();
                    }, $.signalR.transports.longPolling.reconnectDelay + 1500);
                }, 500);
            });
        }, 0);

    });

    // Cleanup
    return function () {
        $.ajax = savedAjax;
        $.network.connect();
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Can reconnect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Reconnecting");
                $.network.connect();
            } else if (connection.state === $.signalR.connectionState.connected) {
                assert.ok(true, "Reconnected");
                end();
            }
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Shifts into reconnecting state.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state.");
                end();
            }
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnecting.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnecting(function () {
            assert.ok(true, "Reconnecting triggered!");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Triggers reconnected.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        connection.reconnecting(function () {
            assert.ok(true, "Reconnecting triggered!");
            $.network.connect();
        });

        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.reconnected(function () {
            assert.ok(true, "Reconnected triggered!");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Clears stop reconnecting timeout on stop inside of stateChanged.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo,
        // Trigger disconnect timeout after X second of trying to reconnect.  This has to be a unique value because
        // we'll be using it to check if we've triggered the disconnect timeout.
        disconnectTimeout = 1.337,
        timeoutId = -1;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Wire up the state changed (while connected) to detect if we connect again
        // In a later test we'll determine if reconnected gets called
        connection.stateChanged(function () {
            if (connection.state === $.signalR.connectionState.reconnecting) {
                assert.ok(true, "Connection now in reconnecting state (via stateChanged), stopping the connection.");
                connection.stop();

                // Set a timeout for 3x the disconnectTimeout.  If it hasn't triggered by then we were successful.
                timeoutId = setTimeout(function () {
                    assert.ok(true, "Stop Reconnect timeout was not triggered, success!");
                    end();
                }, disconnectTimeout * 3);
            }
        });

        $.network.disconnect();
    };

    // Replace log function to see if we trigger reconnectTimeout.  Need to look at the log itself because
    // the rest of the logic is enclosed in a closure.
    connection.log = function (message) {
        // We triggered the disconnect timeout, bad!
        if (message.indexOf(disconnectTimeout) >= 0) {
            assert.ok(false, "Reconnecting Timed out.");
            end();
        }
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        connection.disconnectTimeout = disconnectTimeout;

        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        clearTimeout(timeoutId);
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Can remain connected to /signalr/js.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "signalr/js"),
        demo = connection.createHubProxies().demo,
        testGuidInvocations = 0;

    demo.client.TestGuid = function () {
        testGuidInvocations++;
        if (testGuidInvocations < 2) {
            assert.ok(true, "First testGuid invocation succeeded.");
            demo.server.testGuid();
        } else {
            assert.ok(true, "Second testGuid invocation succeeded.");
            end();
        }
    };

    connection.error(function (e) {
        assert.ok(false, "Connection error: " + e);
        end();
    });

    connection.reconnecting(function () {
        assert.ok(false, "Reconnecting should not be triggered");
        end();
    });

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

// For #1809
QUnit.asyncTimeoutTest("Does not reconnect infinitely if network is down", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo,
        reconnectingTriggered = false;

    // Need to have at least one client function in order to be subscribed to a hub
    demo.client.TestGuid = function () {
        // Stop reconnecting after 6 seconds ( this overrides the negotiate value ).
        connection.disconnectTimeout = 6000;

        connection.reconnecting(function () {
            reconnectingTriggered = true;
        });

        connection.disconnected(function () {
            assert.ok(reconnectingTriggered, "Reconnecting was triggered within the test.");
            assert.ok(true, "Disconnect triggered, transport did not reconnect infinitely.");
            end();
        });

        $.network.disconnect();
    };

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(true, "Connected");
        // Call a server function and request a message back in order to get a message ID so we can successfully reconnect
        demo.server.testGuid();
    });

    // Cleanup
    return function () {
        connection.stop();
        $.network.connect();
    };
});

QUnit.asyncTimeoutTest("Polls exceeding the ConnectionTimeout will trigger a reconnect.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection("echo", end, assert, testName, undefined, false),
        transport = { transport: "longPolling" };

    connection.reconnecting(function () {
        assert.comment("The connection successfully started reconnecting.");
        connection._.pollTimeout = 120000;
    });

    connection.reconnected(function () {
        assert.comment("The connection successfully reconnected.");
        end();
    });

    connection.start(transport).done(function () {
        assert.comment("Connected.");

        // Verify the default pollTimeout is 120 seconds
        // 110 second ConnectionTimeout + 10 second buffer
        assert.equal(connection._.pollTimeout, 120000)

        // Force a new poll that should timeout
        connection._.pollTimeout = 1000;
        connection.send("");
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});