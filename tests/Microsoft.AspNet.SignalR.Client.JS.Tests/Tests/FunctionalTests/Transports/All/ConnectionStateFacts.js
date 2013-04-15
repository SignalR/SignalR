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

    QUnit.asyncTimeoutTest(transport + " transport appends /reconnect to reconnect requests.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connections = [testUtilities.createConnection("multisend", end, assert, testName), testUtilities.createHubConnection(end, assert, testName)],
            getUrlCalled = [false, false],
            numConnections = 2,
            numReconnects = 0,
            savedGetUrl = $.connection.transports._logic.getUrl;

        $.each(connections, function (i, connection) {
            connection.reconnected(function () {
                assert.ok(getUrlCalled[i], "Successfully reconnected");
                numReconnects++;
                if (numReconnects === numConnections) {
                    end();
                }
            });
        });

        $.when.apply($,
            $.map(connections, function (connection) {
                return connection.start({ transport: transport });
            })
        ).done(function () {
            // FIX: The longPolling transport currently needs to receive a message with a message ID before it can "reconnect"
            // Issue #1700
            if (transport === "longPolling") {
                $.each(connections, function (_, connection) {
                    connection.messageId = connection.messageId || "";
                });
            }

            $.connection.transports._logic.getUrl = function (connection) {
                var url = savedGetUrl.apply($.connection.transports._logic, arguments),
                    urlWithoutQS = url.split("?", 1)[0];

                $.each(connections, function (i, conn) {
                    if (conn === connection) {
                        getUrlCalled[i] = true;
                        return false; // Finish looping
                    }
                });

                assert.ok(urlWithoutQS.match(/\/reconnect$/), "URL ends with reconnect");
                return url;
            };

            $.network.disconnect();
            $.network.connect();
        });

        // Cleanup
        return function () {
            $.connection.transports._logic.getUrl = savedGetUrl;
            $.network.connect();
            $.each(connections, function (_, connection) {
                connection.stop();
            });
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
            // Issue #1700
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

    QUnit.asyncTimeoutTest(transport + " transport will attempt to reconnect multiple times.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            reconnectAttempts = 0,
            savedConnectionReconnectDelay = connection.reconnectDelay,
            savedLongPollingReconnectDelay = $.connection.transports.longPolling.reconnectDelay,
            savedReconnect = $.connection.transports[transport].reconnect,
            savedPingServer = $.connection.transports._logic.pingServer;

        function connectIfSecondReconnectAttempt() {
            if (++reconnectAttempts === 2) {
                $.network.connect();
            }
        }

        // Shorten timeouts that slow down reconnect attempts ensure transports attempt reconnecting several times.
        connection.reconnectDelay = 10;
        $.connection.transports.longPolling.reconnectDelay = 10;

        connection.reconnecting(function () {
            assert.equal(connection.state, $.signalR.connectionState.reconnecting, "Transport started reconnecting.");
        });

        // "longPolling will immediately raise the reconnected event, irrespective of the ajax response
        if (transport === "longPolling") {
            connection.reconnected(function () {
                if (reconnectAttempts > 1) {
                    assert.equal(connection.state, $.signalR.connectionState.connected, "Transport reconnected.");
                    end();
                }
            });
        } else {
            connection.reconnected(function () {
                assert.ok(reconnectAttempts > 1, "Transport attempted reconnecting multiple times");
                assert.equal(connection.state, $.signalR.connectionState.connected, "Transport reconnected.");
                end();
            });
        }

        connection.start({ transport: transport }).done(function () {
            assert.equal(connection.state, $.signalR.connectionState.connected, "Connection started.");

            // Monkey patch reconnect functions so we can count how many reconnect attempts have been made
            if (transport === "longPolling") {
                // FIX: The longPolling transport currently needs to receive a message with a message ID before it can "reconnect"
                // Issue #1700
                connection.messageId = connection.messageId || "";
                $.connection.transports._logic.pingServer = function (connection, transport) {
                    return savedPingServer.call($.connection.transports._logic, connection, transport).fail(function () {
                        connectIfSecondReconnectAttempt();
                    });
                };
            } else {
                $.connection.transports[transport].reconnect = function (connection) {
                    savedReconnect.call($.connection.transports[transport], connection);
                    connectIfSecondReconnectAttempt();
                };
            }

            $.network.disconnect();
        });

        return function () {
            connection.reconnectDelay = savedConnectionReconnectDelay;
            $.connection.transports.longPolling.reconnectDelay = savedLongPollingReconnectDelay;

            $.connection.transports[transport].reconnect = savedReconnect;
            $.connection.transports._logic.pingServer = savedPingServer;

            $.network.connect();
            connection.stop();
        };
    });
});