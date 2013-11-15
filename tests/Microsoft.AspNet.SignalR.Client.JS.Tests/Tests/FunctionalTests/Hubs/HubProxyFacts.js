QUnit.module("Hub Proxy Facts");

// All transports will run successfully once #1442 is completed.  At that point we will be able to change this to runWithAllTransports
testUtilities.runWithTransports(["foreverFrame", "serverSentEvents", "webSockets"], function (transport) {
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
            url = connection.url;

        connection.start({ transport: transport }).done(function () {
            assert.isNotSet(connection._.invocationCallbacks["0"], "Callback list should be empty before invocation.");

            // Provide faulty url so the ajaxSend fails.
            connection.url = "http://foo";
            var invokePromise = demo.server.synchronousException();
            // Reset back to original url so background network tasks function properly.
            connection.url = url;

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
            connection.url = url;
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