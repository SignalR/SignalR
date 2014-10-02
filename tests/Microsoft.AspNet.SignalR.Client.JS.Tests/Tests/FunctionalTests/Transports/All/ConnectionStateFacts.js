QUnit.module("Connection State Facts");

testUtilities.runWithAllTransports(function (transport) {

    QUnit.asyncTimeoutTest(transport + " transport triggers start after initialize message.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connections = [testUtilities.createHubConnection(end, assert, testName), testUtilities.createConnection("signalr", end, assert, testName)],
            savedProcessMessage = $.signalR.transports._logic.processMessages,
            runWith = function (connection) {
                var initialized = false,
                    deferred = $.Deferred();

                $.signalR.transports._logic.processMessages = function (connection, minData, onInitialize) {
                    // We could be buffering so ensure that we have the initialize message
                    if (minData.S) {
                        assert.ok(onInitialize, "On initialize passed to process messages.");
                        assert.ok(true, "Initialized");
                        initialized = true;
                    }

                    savedProcessMessage.apply(this, arguments);
                }

                connection.start({ transport: transport }).done(function () {
                    assert.isTrue(initialized, "Start triggered after initialization.");
                    connection.stop();
                    deferred.resolve();
                });

                return deferred.promise();
            };

        runWith(connections[0]).done(function () {
            runWith(connections[1]).done(function () {
                end();
            });
        });

        // Cleanup
        return function () {
            $.signalR.transports._logic.processMessages = savedProcessMessage;
            $.each(connections, function (_, connection) {
                connection.stop();
            });
        };
    });

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
            connection.stop();
            assert.comment("Connection manually stopped, now restarting.");

            // We must wait for a timeout to restart the connection for this test to pass with the long polling transport.
            // Without the timeout, the original polling loop will not terminate.
            window.setTimeout(function () {
                assert.equal($.signalR.connectionState.disconnected, connection.state, "SignalR state is disconnected prior to (re)start.");

                connection.start(activeTransport).done(function () {
                    assert.equal($.signalR.connectionState.connected, connection.state, "SignalR state is connected once start callback is called.");

                    // Wire up the state changed (while connected) to detect if we shift into reconnecting
                    // In a later test we'll determine if reconnected gets called
                    connection.stateChanged(function () {
                        if (connection.state == $.signalR.connectionState.reconnecting) {
                            assert.comment("SignalR state is reconnecting.");
                            end();
                        }
                    });

                    $.network.disconnect();
                });

                assert.equal($.signalR.connectionState.connecting, connection.state, "SignalR state is connecting prior to start deferred resolve.");
            }, 1000);
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
            // We wait 1 second before wiring up the getUrl to ensure that no more requests go through
            window.setTimeout(function () {
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
            }, 1000);
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

    QUnit.asyncTimeoutTest(transport + " transport will attempt to reconnect multiple times.", testUtilities.defaultTestTimeout * 4, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            reconnectAttempts = 0,
            savedGetUrl = $.connection.transports._logic.getUrl;

        function connectIfSecondReconnectAttempt() {
            if (++reconnectAttempts === 2) {
                $.network.connect();
            }
        }

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

            $.connection.transports._logic.getUrl = function () {
                var url = savedGetUrl.apply(this, arguments);

                if (url.indexOf("/reconnect") >= 0) {
                    connectIfSecondReconnectAttempt();
                }

                return url;
            };

            $.network.disconnect();
        });

        return function () {
            $.connection.transports._logic.getUrl = savedGetUrl;

            $.network.connect();
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport failing during start request stops connection.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, null, /*wrapStart*/ false),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            savedJQueryAjax = $.ajax,
            expectedErrorMessage = $.signalR.resources.errorDuringStartRequest,
            errorCount = 0,
            stateChangeCount = 0,
            invokeAjaxStartAndFailTransport = function (ajaxStartThis, ajaxStartArgs) {
                // We want the call to connection.stop() by the InitHandler to abort the start request,
                // not the call to $.network.disconnect() directly.
                $.network.disable();
                savedAjaxStart.apply(ajaxStartThis, ajaxStartArgs);
                $.network.enable();

                $.network.disconnect();
            };

        $.signalR.transports._logic.ajaxStart = function () {
            var ajaxStartThis = this,
                ajaxStartArgs = arguments;

            if (transport === "longPolling") {
                // Wait until there is a poll request that we can fail immediately to ensure
                // that the poll fails before the start request completes successfully.
                $.ajax = function () {
                    savedJQueryAjax.apply(this, arguments);
                    $.ajax = savedJQueryAjax;
                    invokeAjaxStartAndFailTransport(ajaxStartThis, ajaxStartArgs);
                };
            } else {
                invokeAjaxStartAndFailTransport(ajaxStartThis, ajaxStartArgs);
            }
        };

        connection.error(function (error) {
            errorCount++;
            assert.equal(error.message, expectedErrorMessage, "Error callback called with the appropriate error message.");
        });

        connection.stateChanged(function () {
            var expectedStates = [$.signalR.connectionState.connecting, $.signalR.connectionState.disconnected];
            assert.equal(connection.state, expectedStates[stateChangeCount++], "SignalR changed states as expected");
        });

        connection.start({ transport: transport }).fail(function (error) {
            assert.equal(error.message, expectedErrorMessage, "start() failed with the appropriate error message.");

            // Give time for any unexpected errors or state changes
            window.setTimeout(function () {
                assert.equal(errorCount, 1, "The error handler was triggered exactly once.");
                assert.equal(stateChangeCount, 2, "The connection changed states twice.");
                end();
            }, 1000);
        });

        // Cleanup
        return function () {
            $.signalR.transports._logic.ajaxStart = savedAjaxStart;
            $.ajax = savedJQueryAjax;
            $.network.enable();
            $.network.connect();
            connection.stop();
        };
    });
});