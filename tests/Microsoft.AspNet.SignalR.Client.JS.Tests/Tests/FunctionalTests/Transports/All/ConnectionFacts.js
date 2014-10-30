var buildStatusCodeConnection = function (alterWhen, statusCode, end, assert, testName, wrapStart) {
    var connection = testUtilities.createConnection("statusCodeConnection", end, assert, testName, wrapStart);

    connection.qs = {
        alterWhen: alterWhen,
        statusCode: statusCode
    };

    return connection;
};

QUnit.module("Connection Facts");

testUtilities.runWithAllTransports(function (transport) {

    if (!window.document.commandLineTest) {
        QUnit.asyncTimeoutTest(transport + " transport can timeout when it does not receive initialize message.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
                savedProcessMessages = $.signalR.transports._logic.processMessages;

            $.signalR.transports._logic.processMessages = function (_, minData) {
                // Look for initialize message, if we get it, ignore it, transports should time out
                if (!minData.S) {
                    savedProcessMessages.apply(this, arguments);
                }
            };

            connection.start({ transport: transport }).done(function () {
                assert.ok(false, "Connection started");
                end();
            }).fail(function () {
                // transport timed out
                assert.comment("Connection failed to start!");
                end();
            });

            // Cleanup
            return function () {
                $.signalR.transports._logic.processMessages = savedProcessMessages;
                connection.stop();
            };
        });
    }

    QUnit.asyncTimeoutTest(transport + ": Start -> Stop -> Start triggeres the correct deferred's.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            firstFailTriggered = false;

        connection.start({ transport: transport }).done(function () {
            assert.fail("Connection started");
            end();
        }).fail(function () {
            assert.comment("Fail handler was triggered on aborted negotiate.");
            firstFailTriggered = true;
        });

        connection.stop();

        connection.start({ transport: transport }).done(function () {
            assert.comment("Connection started successfully.");
            assert.isTrue(firstFailTriggered, "First connections fail was triggered.");

            end();
        }).fail(function () {
            assert.fail("Connection failed to start.");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Ping interval stops the connection on 401's.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = buildStatusCodeConnection("ping", 401, end, assert, testName, false);

        connection.error(function (error) {
            assert.equal(error.message, $.signalR._.format($.signalR.resources.pingServerFailedStatusCode, 401), "Failed to ping server due to 401.");

            setTimeout(function () {
                assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection was stopped.");
                end();
            }, 500);
        });

        // Start the connection and ping the server every 1 second
        connection.start({ transport: transport, pingInterval: 1000 });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Ping interval stops the connection on 403's.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = buildStatusCodeConnection("ping", 403, end, assert, testName, false);

        connection.error(function (error) {
            assert.equal(error.message, $.signalR._.format($.signalR.resources.pingServerFailedStatusCode, 403), "Failed to ping server due to 403.");

            setTimeout(function () {
                assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection was stopped.");
                end();
            }, 500);
        });

        // Start the connection and ping the server every 1 second
        connection.start({ transport: transport, pingInterval: 1000 });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Ping interval behaves appropriately.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            savedPingServer = $.signalR.transports._logic.pingServer,
            pingCount = 0;

        $.signalR.transports._logic.pingServer = function () {
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
            if (url.indexOf("/ping") === -1 && (url.indexOf("connectionData=") === -1 || url.toLowerCase().indexOf("connectiondataverifierhub") === -1)) {
                connectionDataVerifierHub.client.fail();
            }

            // Persist the request through to the original ajax request
            return savedAjax.call(this, url, settings);
        };

        connectionDataVerifierHub.client.pong = function () {
            // Verify that ping server flows through the connection data
            $.signalR.transports._logic.pingServer(connection).done(function () {
                assert.comment("All requests contained connection data within the query string.");
                end();
            }).fail(function () {
                connectionDataVerifierHub.client.fail();
            });
        };

        connectionDataVerifierHub.client.fail = function () {
            assert.fail("Query string did not contain connection data.");
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
            }, 0);
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Reconnect exceeding the reconnect window results in the connection disconnecting even with a fast beat interval.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            handle,
            onErrorFiredForTimeout = false;

        connection.error(function (err) {
            assert.comment("Error fired.");
            if (err.source === "TimeoutException") {
                onErrorFiredForTimeout = true;
            }
        });

        connection.disconnected(function () {
            assert.comment("Disconnected fired.");

            // Let callstack finish
            setTimeout(function () {
                end();
            }, 0);

            assert.ok(onErrorFiredForTimeout);
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
            connection.reconnectWindow = 500;
        });

        connection.disconnected(function () {
            assert.comment("Disconnected fired.");
            assert.equal(connection.lastError.source, "TimeoutException", "Disconnected event has expected close reason");
            end();
        });

        connection.start({ transport: transport }).done(function () {
            connection._.beatInterval = 5000;

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

    QUnit.asyncTimeoutTest(transport + ": Start deferred triggers immediately after start.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false);

        connection.start({ transport: transport }).done(function () {
            var triggeredOnNextStack = false;

            connection.start().done(function () {
                triggeredOnNextStack = true;
            });

            // Start a new timeout to run to be the NEXT stack.  A resolved deferred should
            // create a new stack and this should be run after that.
            setTimeout(function () {
                assert.isTrue(triggeredOnNextStack, "Start deferred triggered instantly.");
                end();
            }, 0);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Start can be called multiple times.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var startCount = 3,
            connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            connectionsStarted = 0;

        for (var i = 0; i < startCount; i++) {
            connection.start({ transport: transport }).done(function () {
                if (++connectionsStarted) {
                    assert.comment("All connections started functions triggered successfully.");
                    assert.equal(connection.state, $.signalR.connectionState.connected, "Connection connected");
                    end();
                }
            });
        }

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Connections can be started and stopped repeatedly without errors.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var restartCount = 3,
            failTriggered = false,
            connection = testUtilities.createHubConnection(end, assert, testName, undefined, false);

        connection.error(function () {
            assert.fail("Error was triggered.");
        });

        for (var i = 0; i < restartCount; i++) {
            connection.start({ transport: transport }).done(function () {
                assert.fail("Connection started");
                end();
            }).fail(function () {
                failTriggered = true;
            });

            connection.stop();
            assert.isTrue(failTriggered, "Fail handler was triggered on aborted negotiate.");
            failTriggered = false;
        }

        window.setTimeout(function () {
            assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection is disconnected.");
            assert.equal(connection.transport, null, "Transport was not instantiated and is cleared.");
            end();
        }, 3000);

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest("Connection can be stopped during " + transport + " transport start.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false);

        // Triggered right before transports begin to start.
        connection.starting(function () {
            connection.stop();
        });

        connection.error(function () {
            assert.fail("Error was triggered.");
        });

        connection.start({ transport: transport }).done(function () {
            assert.fail("Connection started");
            end();
        }).fail(function () {
            assert.comment("Fail handler was triggered.");
        });

        window.setTimeout(function () {
            assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection is disconnected.");
            assert.equal(connection.transport, null, "Transport was not instantiated and is cleared.");
            end();
        }, 2000);

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest("Connection can be stopped prior to " + transport + " transport start.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false);

        connection.start({ transport: transport }).done(function () {
            assert.fail("Connection started");
            end();
        }).fail(function () {
            assert.comment("Fail handler was triggered on aborted negotiate.");
        });

        connection.error(function () {
            assert.fail("Error was triggered.");
        });

        connection.stop();

        window.setTimeout(function () {
            assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection is disconnected.");
            assert.equal(connection.transport, null, "Transport was not instantiated and is cleared.");
            end();
        }, 2000);

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
            return savedAjax.call(this, url, settings);
        };

        $.ajax = ajaxReplacement;
        var errorFired = false;

        connection.start({ transport: transport }).fail(function () {
            assert.ok(errorFired, "Protocol version error thrown");
            end();
        });

        connection.error(function (err) {
            errorFired = true;
            assert.equal(err.message, "You are using a version of the client that isn't compatible with the server. Client version " + connection.clientProtocol + ", server version 1.1.",
                "Protocol version error message thrown");
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport auto JSON encodes messages correctly when sending.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("echo", end, assert, testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 5) {
                $.each(values, function (index, val) {
                    var decoded;
                    if (val === undefined || val === null) {
                        return;
                    } else if (val.indexOf("1.") >= 0) {
                        assert.equal(val, "1.test", "Raw string correctly sent not JSON encoded");
                    } else if (val.indexOf("2.") >= 0) {
                        decoded = JSON.parse(val);
                        assert.equal(decoded.test, "2.test", "Object correctly sent JSON encoded");
                    } else if (val.indexOf("3.") >= 0) {
                        decoded = JSON.parse(val);
                        assert.equal(decoded[0], "3.test", "Array correctly sent JSON encoded");
                    }
                });
                end();
            }
        });

        connection.start({ transport: transport }).done(function () {
            connection.send("1.test");
            connection.send({ test: "2.test" });
            connection.send(["3.test"]);
            connection.send(null);
            connection.send(undefined);
        });

        // Cleanup
        return function () {
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
