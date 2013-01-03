QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and are able to send data.", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        proxies = connection.createHubProxies(),
        demo = proxies.demo;

    connection.start().done(function () {
        assert.ok(true, "Connected");
        demo.server.overload(6).done(function (val) {
            assert.equal(val, 6, "Successful return value from server via send");
            end();
        });
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});