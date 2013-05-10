﻿QUnit.module("Core - connection.json Facts");

(function ($, window) {
    testUtilities.runWithAllTransports(function (transport) {

        QUnit.asyncTimeoutTest(transport + " transport uses custom JSON object.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connections = [testUtilities.createConnection("signalr", end, assert, testName),
                               testUtilities.createHubConnection(end, assert, testName)],
                numStarts = 0;

            $.each(connections, function (_, connection) {
                var customJsonCalled = false;

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
                    numStarts++;
                    if (numStarts === connections.length) {
                        end();
                    }
                });
            });

            return function () {
                $.each(connections, function (_, connection) {
                    connection.stop();
                });
            };
        });

        // This is not a unit test because in the case that it fails it could run async
        QUnit.asyncTimeoutTest(transport + ": connection throws if no valid json parser found.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connections = [testUtilities.createConnection("signalr", end, assert, testName),
                               testUtilities.createHubConnection(end, assert, testName)],
                throwCount = 0,
                numStarts = 0;

            $.each(connections, function (_, connection) {
                numStarts++;

                connection.json = undefined;

                try {
                    connection.start({ transport: transport });
                }
                catch (ex) {
                    throwCount++;
                    assert.ok(true, "Connection throws when no valid json is found.");
                    if (numStarts === connections.length) {
                        window.setTimeout(function () {
                            end();
                        }, 0);
                    }
                }

                assert.equal(throwCount, numStarts, "Connection threw exception on start.");
            });

            return function () {
                $.each(connections, function (_, connection) {
                    connection.stop();
                });
            };
        });

    });
})($, window);