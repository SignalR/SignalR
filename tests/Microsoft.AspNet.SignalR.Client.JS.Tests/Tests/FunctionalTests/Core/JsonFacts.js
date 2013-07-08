QUnit.module("Core - connection.json Facts");

(function ($, window) {
    testUtilities.runWithAllTransports(function (transport) {
        QUnit.asyncTimeoutTest(transport + " transport uses custom JSON object.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName),
                customJsonCalled = false;
            
            connection.json = {
                parse: function () {
                    customJsonCalled = true;
                    return window.JSON.parse.apply(window, arguments);
                },
                stringify: function () {
                    customJsonCalled = true;
                    return window.JSON.stringify.apply(window, arguments);
                }
            };

            connection.start({ transport: transport }).done(function () {
                assert.ok(customJsonCalled, "Should use custom JSON object.");
                end();
            });

            return function () {
                connection.stop();
            };
        });
    });

    testUtilities.runWithTransports(["longPolling", "serverSentEvents", "webSockets"], function (transport) {

        QUnit.asyncTimeoutTest(transport + ": Invalid JSON payloads triggers connection error.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createHubConnection(end, assert, testName),
                echoHub = connection.createHubProxies().echoHub;

            echoHub.client.echo = function (value) {
                assert.fail("Echo successfully triggered.");
                end();
            };

            connection.start({ transport: transport }).done(function () {
                assert.comment("Connected.");

                connection.json = {
                    parse: function () {
                        arguments[0] += "invaaallllid";
                        return window.JSON.parse.apply(window, arguments);
                    },
                    stringify: function () {
                        return window.JSON.stringify.apply(window, arguments);
                    }
                };

                connection.error(function () {
                    assert.comment("Error was triggered after bad data.");
                    end();
                });

                echoHub.server.echoCallback("hello");
            });

            // Cleanup
            return function () {
                connection.stop();
            };
        });
    });

})($, window);