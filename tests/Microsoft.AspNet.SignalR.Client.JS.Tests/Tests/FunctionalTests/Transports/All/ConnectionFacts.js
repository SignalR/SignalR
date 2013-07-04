QUnit.module("Connection Facts");

testUtilities.runWithAllTransports(function (transport) {

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
            connection = testUtilities.createHubConnection(end, assert, testName, undefined, false);

        for (var i = 0; i < restartCount; i++) {
            connection.start({ transport: transport }).done(function () {
                assert.fail("Connection started");
                end();
            }).fail(function () {
                assert.fail("Connections deferred was rejected.");
            });

            connection.stop();
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
            // Let the current stack unwind and then stop, this will result in us stopping right after the transport has started its networking layer
            setTimeout(function () {
                connection.stop();
            }, 0);
        });

        connection.start({ transport: transport }).done(function () {
            assert.fail("Connection started");
            end();
        }).fail(function () {
            assert.fail("Connections deferred was rejected.");
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
            assert.fail("Connections deferred was rejected.");
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

    QUnit.asyncTimeoutTest(transport + " transport can timeout when it does not receive initialize message.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
            savedProcessMessages = $.signalR.transports._logic.processMessages;

        $.signalR.transports._logic.processMessages = function (_, minData) {
            // Look for initialize message, if we get it, ignore it, transports should time out
            if (!minData.S) {
                savedProcessMessages.apply(this, arguments);
            }
        }

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
            assert.equal(err, "You are using a version of the client that isn't compatible with the server. Client version " + connection.clientProtocol + ", server version 1.1.",
                "Protocol version error message thrown");
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport auto JSON encodes messages correctly when sending.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("autoencodedjson", end, assert, testName),
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

