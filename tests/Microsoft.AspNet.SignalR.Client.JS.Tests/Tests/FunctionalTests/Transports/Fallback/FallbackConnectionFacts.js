QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start().done(function () {
        assert.ok(true, "Connected");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

// 1 test timeout per transport
QUnit.asyncTimeoutTest("Connection times out when initialize not received.", testUtilities.defaultTestTimeout*4, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        savedProcessMessages = $.signalR.transports._logic.processMessages;

    $.signalR.transports._logic.processMessages = function (_, minData) {
        // Look for initialize message, if we get it, ignore it, transports should time out
        if (!minData.S) {
            savedProcessMessages.apply(this, arguments);
        }
    }

    connection.start().done(function () {
        assert.ok(false, "Connection started");
        end();
    }).fail(function () {
        // All transports fell back because they did not get init message, success!
        assert.comment("Connection failed to start!");
        end();
    });

    // Cleanup
    return function () {
        $.signalR.transports._logic.processMessages = savedProcessMessages;
        connection.stop();
    };
});

QUnit.module("Fallback Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSockets fall back to next transport.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);
    var saveWebSocket = window.WebSocket;
    window.WebSocket = function () {
        this.close = $.noop;
    };
    connection.start().done(function () {
        assert.notEqual(connection.transport.name, "webSockets", "Connected using " + connection.transport.name);
        end();
    });

    // Cleanup
    return function () {
        window.WebSocket = saveWebSocket;
        connection.stop();
    };
});

QUnit.module("Fallback Facts", testUtilities.transports.webSockets.enabled && $.signalR._.ieVersion >= 10);

QUnit.asyncTimeoutTest("WebSockets fall back to next transport when connection limit exceeded.", testUtilities.defaultTestTimeout * 3, function (end, assert, testName) {
    var connections = [],
        connectionMax = 6,
        createAndAdd = function () {
            var connection = testUtilities.createHubConnection(end, assert, testName);

            connections.push(connection);

            return connection;
        },
        startFallbackConnection = function () {
            var connection = createAndAdd();

            connection.start().done(function () {
                assert.equal(connection.transport.name, "foreverFrame", "Transport fell back to foreverFrame on security error.");
                end();
            });
        },
        startConnection = function () {
            var connection = createAndAdd(),
                onDone = (connections.length < connectionMax) ? function () {
                    // We only want to assert websockets on the first connection because if previous tests recently finished their websocket connections
                    // they could still be in the process of tearing the connections down, resulting in an early security error.
                    if (connections.length === 1) {
                        assert.equal(connection.transport.name, "webSockets", "First connection is web sockets (prior to security error).");
                    }

                    startConnection();
                } : startFallbackConnection;

            connection.start().done(onDone);
        };

    $.network.disable();

    // Start the connections
    startConnection();

    // Cleanup
    return function () {
        for (var i = 0; i < connections.length; i++) {
            connections[i].stop();
        }
        $.network.enable();
    };
});

QUnit.asyncTimeoutTest("OnConnected fires once when WebSockets falls back", testUtilities.defaultTestTimeout * 3, function (end, assert, testName, undefined) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        savedProcessMessages = $.signalR.transports._logic.processMessages,
        statusHub = connection.createHubProxies().StatusHub,
        onConnectedEventCallCount = 0;

    statusHub.client.joined = function (connectionId) {
        if (connection.id === connectionId) {
            onConnectedEventCallCount++;
        }
    };

    $.signalR.transports._logic.processMessages = function (_, minData) {
        // Look for initialize message, if we get it, ignore it, transports should time out
        if (connection.transport.name === "webSockets") {
            minData.S = undefined;
        }
        savedProcessMessages.apply(this, arguments);
    }

    connection.start().done(function () {
        assert.equal(onConnectedEventCallCount, 1, "OnConnected fired once.");
        end();
    });

    // Cleanup
    return function () {
        $.signalR.transports._logic.processMessages = savedProcessMessages;
        connection.stop();
    };
});
