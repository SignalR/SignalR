QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and are able to send data.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        proxies = connection.createHubProxies(),
        demo = proxies.demo;

    connection.start().done(function () {
        assert.ok(true, "Connected");
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