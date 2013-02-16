QUnit.module("Hub Proxy Facts");

// All transports will run successfully once #1442 is completed.  At that point we will be able to change this to runWithAllTransports
testUtilities.runWithTransports(["foreverFrame", "serverSentEvents", "webSockets"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName),
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
});

if (!window.document.commandLineTest) {
    // Replacing window.onerror will not capture uncaught errors originating from inside an iframe
    testUtilities.runWithTransports(["longPolling", "serverSentEvents", "webSockets"], function (transport) {
        QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in invocation callbacks.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(testName),
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
            }).fail(function (reason) {
                assert.ok(false, "Failed to initiate SignalR connection");
                end();
            });

            // Cleanup
            return function () {
                window.onerror = onerror;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in client hub methods.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(testName),
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
            }).fail(function (reason) {
                assert.ok(false, "Failed to initiate SignalR connection");
                end();
            });

            // Cleanup
            return function () {
                window.onerror = onerror;
                connection.stop();
            };
        });
    });
}