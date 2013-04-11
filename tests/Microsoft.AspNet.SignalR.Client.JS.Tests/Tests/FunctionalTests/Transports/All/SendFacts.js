QUnit.module("Send Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport can send ", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo;

        // Must subscribe to at least one method on client
        demo.client.foo = function () { };

        connection.start({ transport: transport }).done(function () {
            demo.server.overload(6).done(function (val) {
                assert.equal(val, 6, "Successful return value from server via send");
                end();
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " can transmit primitive types correctly ", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName);

        connection.received(function (data) {
            assert.equal(typeof (data), "string", "Primitive type data received in the correct type at the client");
            end();
        });

        connection.start({ transport: transport });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});
