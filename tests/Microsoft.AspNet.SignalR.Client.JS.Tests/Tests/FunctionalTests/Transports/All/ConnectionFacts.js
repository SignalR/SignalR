QUnit.module("Connection Facts");

testUtilities.runWithAllTransports(function (transport) {
    // Cannot run with long polling because it uses the ping server as its initial init
    if (transport !== "longPolling") {
        QUnit.asyncTimeoutTest(transport + ": Connection data flows with all requests to server.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
                connectionDataVerifierHub = connection.createHubProxies().connectionDataVerifierHub,
                savedAjax = $.ajax,
                transportSettings = {
                    transport: transport
                };

            $.ajax = function (url, settings) {
                if (!settings) {
                    settings = url;
                    url = settings.url;
                }

                // Check if it's the ping request;
                if (url.indexOf("connectionData=") === -1 || url.toLowerCase().indexOf("connectiondataverifierhub") === -1) {
                    connectionDataVerifierHub.client.fail();
                }

                // Persist the request through to the original ajax request
                return savedAjax.call(this, url, settings);
            };

            connectionDataVerifierHub.client.pong = function () {
                // Verify that ping server flows through the connection data
                $.signalR.transports._logic.pingServer(connection, transport).done(function () {
                    assert.ok(true, "All requests contained connection data within the query string.");
                    end();
                }).fail(function () {
                    connectionDataVerifierHub.client.fail();
                });
            };

            connectionDataVerifierHub.client.fail = function () {
                assert.ok(false, "Query string did not contain connection data.");
                end();
            };

            connection.reconnected(function () {
                connectionDataVerifierHub.server.ping();
            });

            connection.start(transportSettings).done(function () {
                // Test disconnected
                connection.stop();

                setTimeout(function () {
                    connection.start(transportSettings).done(function () {
                        // Delay the network disconnect so that transports can be 100% conneted with disconnect is triggered
                        setTimeout(function () {
                            // Cause a reconnect
                            $.network.disconnect();
                            $.network.connect();
                        }, 250);
                    });
                }, 2000);
            });

            // Cleanup
            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });
    }

    QUnit.asyncTimeoutTest(transport + ": Reconnect exceeding the reconnect window results in the connection disconnecting even with a fast beat interval.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            handle;

        connection.disconnected(function () {
            assert.comment("Disconnected fired.");

            // Let callstack finish
            setTimeout(function () {
                end();
            },0);
        });

        connection.start({ transport: transport }).done(function () {
            connection._.beatInterval = 100;
            connection.reconnectWindow = 0;
            connection.reconnectDelay = testUtilities.defaultTestTimeout + 1000;

            // Wait for the transports to settle (no messages flowing)
            handle = setTimeout(function () {
                $.network.disconnect();
            }, 1000);
        });

        // Cleanup
        return function () {
            clearTimeout(handle);
            $.network.connect();
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Reconnect exceeding the reconnect window results in the connection disconnecting.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            handle;

        connection.reconnecting(function () {
            assert.comment("Reconnecting fired.");
        });

        connection.disconnected(function () {
            assert.comment("Disconnected fired.");
            end();
        });

        connection.start({ transport: transport }).done(function () {
            connection._.beatInterval = 5000;

            // Wait for the transports to settle (no messages flowing)
            handle = setTimeout(function () {
                connection.reconnectWindow = 0;
                $.network.disconnect();
                $.network.connect();
            }, 1000);
        });

        // Cleanup
        return function () {
            clearTimeout(handle);
            $.network.connect();
            connection.stop();
        };
    });

    // Can't run this for long polling because it still uses a ping server to begin its connection
    if (transport !== "longPolling") {
        QUnit.asyncTimeoutTest(transport + ": Ping interval behaves appropriately.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
                savedPingServer = $.signalR.transports._logic.pingServer,
                pingCount = 0;

            $.signalR.transports._logic.pingServer = function (connection) {
                pingCount++;

                return savedPingServer.apply(this, arguments);
            };

            // Start the connection and ping the server every 1 second
            connection.start({ transport: transport, pingInterval: 1000 }).done(function () {
                setTimeout(function () {
                    var currentPingCount = pingCount;

                    assert.ok(currentPingCount >= 2, "Ping server was triggered at least 2 times");
                    connection.stop();
                    setTimeout(function () {
                        assert.equal(currentPingCount, pingCount, "After calling stop ping interval no longer runs.");

                        connection.start({ transport: transport, pingInterval: 500 }).done(function () {
                            setTimeout(function () {
                                assert.equal(currentPingCount + 1, pingCount, "After restarting the connection the ping interval can be reconfigured and continues execution.");
                                end();
                            }, 900);
                        });
                    }, 1500);
                }, 3000);
            });

            // Cleanup
            return function () {
                $.signalR.transports._logic.pingServer = savedPingServer;
                connection.stop();
            };
        });
    }

    QUnit.asyncTimeoutTest(transport + ": Start -> Stop -> Start triggeres the correct deferred's.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection($.noop, { ok: $.noop }, testName),
            firstFailTriggered = false;

        connection.start({ transport: transport }).done(function () {
            assert.ok(false, "Connection started");
            end();
        }).fail(function () {
            assert.ok(true, "Fail handler was triggered on aborted negotiate.");
            firstFailTriggered = true;
        });

        connection.stop();

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connection started successfully.");
            assert.ok(firstFailTriggered, "First connections fail was triggered.");

            end();
        }).fail(function () {
            assert.ok(false, "Connection failed to start.");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport can send and receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 4) {
                assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
                assert.equal(values[2], "OnReceivedAsync1", "Received OnReceivedAsync1");
                assert.equal(values[3], "OnReceivedAsync2", "Received OnReceivedAsync2");
                end();
            }
        });

        connection.error(function (err) {
            assert.ok(false, "Error raised");
            end();
        });

        connection.start({ transport: transport }).done(function () {
            connection.send("test");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport can receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 2) {
                assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
                end();
            }
        });

        connection.start({ transport: transport });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport throws an error if protocol version is incorrect", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", $.noop, { ok: $.noop }, testName),
            savedAjax = $.ajax;

        function ajaxReplacement(url, settings) {
            var savedSuccess;

            if (!settings) {
                settings = url;
                url = settings.url;
            }

            // Check if it's the negotiate request
            if (url.indexOf("/negotiate") >= 0) {
                // Let the ajax request finish out
                savedSuccess = settings.success;
                settings.success = function (result) {
                    var res = connection._parseResponse(result);
                    res.ProtocolVersion = "1.1";
                    result = connection.json.stringify(res);
                    savedSuccess.apply(this, arguments);
                }
            }

            // Persist the request through to the original ajax request
            savedAjax.call(this, url, settings);
        };

        $.ajax = ajaxReplacement;
        var errorFired = false;

        connection.start({ transport: transport }).fail(function () {
            assert.ok(errorFired, "Protocol version error thrown");
            end();
        });

        connection.error(function (err) {
            errorFired = true;
            assert.equal(err, "You are using a version of the client that isn't compatible with the server. Client version 1.2, server version 1.1.",
                "Protocol version error message thrown");
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });
});

QUnit.module("Connection Facts", !window.document.commandLineTest);

// Replacing window.onerror will not capture uncaught errors originating from inside an iframe
testUtilities.runWithTransports(["longPolling", "serverSentEvents", "webSockets"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in onReceived.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            onerror = window.onerror;

        window.onerror = function (errorMessage) {
            assert.ok(errorMessage.match(/onReceived error/));
            end();
            return true;
        }

        connection.received(function (data) {
            throw new Error("onReceived error");
        });

        connection.start({ transport: transport });

        // Cleanup
        return function () {
            window.onerror = onerror;
            connection.stop();
        };
    });

});

