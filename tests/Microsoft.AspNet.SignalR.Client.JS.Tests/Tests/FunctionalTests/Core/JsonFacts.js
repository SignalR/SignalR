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
})($, window);