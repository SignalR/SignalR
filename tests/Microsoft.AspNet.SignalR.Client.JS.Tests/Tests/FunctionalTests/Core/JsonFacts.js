QUnit.module("Core - connection.json Facts");

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
    });
})($, window);