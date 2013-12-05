QUnit.module("Hub Proxy Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection($.noop, { ok: $.noop }, testName),
            badHub = connection.createHubProxy('SomeHubThatDoesntExist');

        badHub.on("foo", function () { });

        connection.start({ transport: transport }).done(function () {
            assert.ok(false, "SignalR connection started with bad hub.");
            end();
        }).fail(function () {
            assert.ok(true, "Success! Failed to initiate SignalR connection with bad hub");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport buffers messages correctly.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            onConnectedBufferHub = connection.createHubProxies().onConnectedBufferHub,
            bufferMeCalls = 0,
            lastBufferMeValue = -1;

        onConnectedBufferHub.client.pong = function () {
            assert.equal(bufferMeCalls, 2, "Buffer me has been called twice prior to pong's execution.");
            end();
        };

        onConnectedBufferHub.client.bufferMe = function (val) {
            // Ensure correct ordering of the buffered messages
            assert.isTrue(lastBufferMeValue < val, "Buffered message is drained in the correct order.");
            lastBufferMeValue = val;
            bufferMeCalls++;
            assert.equal(connection.state, $.signalR.connectionState.connected, "Buffer me triggers after the connection is in the connected state.");
        };

        // Issue #2595
        connection.received(function () { });

        connection.start({ transport: transport }).done(function () {
            assert.equal(bufferMeCalls, 2, "After start's deferred completes the buffer has already been drained.");

            onConnectedBufferHub.server.ping();
        });
        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " hub connection clears invocation callbacks after successful invocation.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo;

        connection.start({ transport: transport }).done(function () {
            assert.isNotSet(connection._.invocationCallbacks["0"], "Callback list should be empty before invocation.");

            var invokePromise = demo.server.overload(100);
            
            assert.isSet(connection._.invocationCallbacks["0"], "Callback should be in the callback list.");

            invokePromise.done(function (result) {
                assert.isNotSet(connection._.invocationCallbacks["0"], "Callback should be cleared.");
                end();
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " hub connection clears invocation callbacks after failed invocation.", testUtilities.defaultTestTimeout * 3, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            token = connection.token;

        connection.start({ transport: transport }).done(function () {
            assert.isNotSet(connection._.invocationCallbacks["0"], "Callback list should be empty before invocation.");

            // Provide faulty token so the ajaxSend fails.
            connection.token = "hello world";
            var invokePromise = demo.server.synchronousException();
            // Reset back to original token so background network tasks function properly.
            connection.token = token;

            assert.isSet(connection._.invocationCallbacks["0"], "Callback should be in the callback list.");

            invokePromise.done(function (result) {
                assert.fail("Invocation succeeded.");
                end();
            }).fail(function () {
                assert.isNotSet(connection._.invocationCallbacks["0"], "Callback should be cleared.");
                end();
            });
        });

        // Cleanup
        return function () {
            // Replace url with a valid url so stop completes successfully.
            connection.token = token;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " hub connection clears all invocation callbacks on stop.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            connectionStopping = false;

        connection.start({ transport: transport }).done(function () {
            demo.server.getValue()
                .done(function () {
                    assert.ok(false, "Method invocation returned after connection stopped.");
                    end();
                })
                .fail(function () {
                    assert.ok(connectionStopping, "Method invocation promise should be rejected when connection is stopped.");
                });

            connectionStopping = true;
            connection.stop();
            connectionStopping = false;

            assert.equal(connection._.invocationCallbackId, 0, "Callback id should be reset to zero.");
            assert.isNotSet(connection._.invocationCallbacks["0"], "Callbacks should be cleared.");

            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " detailed errors are always given for hub exceptions", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, "signalr2/test"),
            demo = connection.createHubProxies().demo;

        connection.start({ transport: transport }).done(function () {
            demo.server.hubException()
                .done(function () {
                    assert.fail("Invocation succeeded but should have failed.");
                    end();
                })
                .fail(function (error) {
                    assert.equal(error.message, "message", "The error message should be 'message'.");
                    assert.equal(error.data, "errorData", "The error.data property should be 'errorData'.");
                    assert.equal(error.source, "HubException", "The error.source property should be 'HubException'");
                    end();
                });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " detailed errors are always given for hub exceptions without error data", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, "signalr2/test"),
            demo = connection.createHubProxies().demo;

        connection.start({ transport: transport }).done(function () {
            demo.server.hubExceptionWithoutErrorData()
                .done(function () {
                    assert.fail("Invocation succeeded but should have failed.");
                    end();
                })
                .fail(function (error) {
                    assert.equal(error.message, "message", "The error message should be 'message'.");
                    assert.isNotSet(error.data, "The error.data property should be absent.");
                    assert.equal(error.source, "HubException", "The error.source property should be 'HubException'");
                    end();
                });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});

QUnit.module("Hub Proxy Facts", !window.document.commandLineTest);

// Replacing window.onerror will not capture uncaught errors originating from inside an iframe
testUtilities.runWithTransports(["longPolling", "serverSentEvents", "webSockets"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in invocation callbacks.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            onerror = window.onerror;

        window.onerror = function (errorMessage) {
            assert.ok(errorMessage.match(/overload error/));
            end();
            return true;
        }

        connection.start({ transport: transport }).done(function () {
            demo.server.overload().done(function () {
                throw new Error("overload error");
            });
        });

        // Cleanup
        return function () {
            window.onerror = onerror;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in client hub methods.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            onerror = window.onerror;

        window.onerror = function (errorMessage) {
            assert.ok(errorMessage.match(/error in callback/));
            end();
            return true;
        }

        demo.client.errorInCallback = function () {
            throw new Error("error in callback");
        }

        connection.start({ transport: transport }).done(function () {
            demo.server.doSomethingAndCallError();
        });

        // Cleanup
        return function () {
            window.onerror = onerror;
            connection.stop();
        };
    });
});