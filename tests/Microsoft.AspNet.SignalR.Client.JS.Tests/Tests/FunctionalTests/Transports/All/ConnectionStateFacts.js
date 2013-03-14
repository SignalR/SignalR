QUnit.module("Connection State Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport connection shifts into appropriate states.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo;

        // Need to have at least one client function in order to be subscribed to a hub
        demo.client.foo = function () { };

        assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to start.");

        connection.start({ transport: transport }).done(function () {
            assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

            // Wire up the state changed (while connected) to detect if we shift into reconnecting
            // In a later test we'll determine if reconnected gets called
            connection.stateChanged(function () {
                if (connection.state == $.signalR.connectionState.reconnecting) {
                    assert.ok(true, "SignalR state is reconnecting.");
                    end();
                }
            });

            $.network.disconnect();
        });

        assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");

        // Cleanup
        return function () {
            $.network.connect();
            connection.stop();
        };
    });


    QUnit.asyncTimeoutTest(transport + " transport connection StateChanged event is called for every state", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            statesSet = {};

        // Preset all state values to false
        for (var key in $.signalR.connectionState) {
            statesSet[$.signalR.connectionState[key]] = 0;
        }

        connection.stateChanged(function () {
            statesSet[connection.state]++;

            if (connection.state == $.signalR.connectionState.reconnecting) {
                connection.stop();

                for (var key in $.signalR.connectionState) {
                    assert.equal(statesSet[$.signalR.connectionState[key]], 1, "SignalR " + key + " state was called via state changed exactly once.");
                }
                end();
            }
        });

        // Need to have at least one client function in order to be subscribed to a hub
        demo.client.foo = function () { };

        connection.start({ transport: transport }).done(function () {
            $.network.disconnect();
        });

        // Cleanup
        return function () {
            $.network.connect();
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport Manually restarted client maintains consistent state.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            activeTransport = { transport: transport };

        // Need to have at least one client function in order to be subscribed to a hub
        demo.client.foo = function () { };

        connection.start(activeTransport).done(function () {
            setTimeout(function () {
                // Synchronously stop
                connection.stop(false);

                assert.ok(true, "Connection manually stopped, now restarting.");

                assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to (re)start.");

                connection.start(activeTransport).done(function () {
                    assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

                    // Wire up the state changed (while connected) to detect if we shift into reconnecting
                    // In a later test we'll determine if reconnected gets called
                    connection.stateChanged(function () {
                        if (connection.state == $.signalR.connectionState.reconnecting) {
                            assert.ok(true, "SignalR state is reconnecting.");
                            end();
                        }
                    });

                    $.network.disconnect();
                });

                assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");
            }, 250);
        });

        // Cleanup
        return function () {
            $.network.connect();
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport supports multiple simultaneous reconnecting connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var numConnections = 2,
            connections = [],
            i;

        function verifyState(stateName) {
            var valid = 0,
                expectedState = $.signalR.connectionState[stateName],
                i;

            for (i = 0; i < numConnections; i++) {
                if (connections[i].state === expectedState) {
                    valid++;
                } else {
                    assert.ok(false, "Connection " + i + " is in state " + connections[i].state + ", but is expected to be in state " + expectedState);
                }
            }
            assert.equal(valid, numConnections, valid + " connections of " + numConnections + " in " + stateName + " state.");
            if (valid !== numConnections) {
                end();
            }
        }

        function createPromise(eventName) {
            var deferreds = [],
                promises = [];

            $.each(connections, function (key, connection) {
                deferreds[key] = $.Deferred(); 
                promises[key] = deferreds[key].promise();
                connection[eventName](function () {
                    deferreds[key].resolve();
                });
            });

            return $.when.apply($, promises);
        }

        for (i = 0; i < numConnections; i++) {
            connections[i] = testUtilities.createHubConnection(end, assert, testName + " (connection " + i + ")");
        }

        $.when.apply($,
            $.map(connections, function (connection) {
                return connection.start({ transport: transport });
            })
        ).pipe(function () {
            var promise = createPromise("reconnecting");
            verifyState("connected");

            // FIX: The longPolling transport currently needs to receive a message with a message ID before it can "reconnect"
            if (transport === "longPolling") {
                $.each(connections, function (_, connection) {
                    connection.messageId = connection.messageId || "";
                });
            }

            $.network.disconnect();
            return promise;
        }).pipe(function () {
            verifyState("reconnecting");
            $.network.connect();
            return createPromise("reconnected");
        }).done(function () {
            verifyState("connected");
            end();
        });


        // Cleanup
        return function () {
            $.network.connect();
            for (var i = 0; i < numConnections; i++) {
                connections[i].stop();
            }
        };
    });
});