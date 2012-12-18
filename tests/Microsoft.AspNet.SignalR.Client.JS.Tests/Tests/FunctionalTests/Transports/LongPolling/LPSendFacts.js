module("Long Polling Facts");

QUnit.asyncTimeoutTest("Long Polling can send ", 5000, function (end) {
    var connection = testUtilities.createHubConnection(),
        proxies = connection.createHubProxies(),
        demo = proxies.demo;

    // Must subscribe to at least one method on client
    demo.client.foo = function () {};

    connection.start().done(function () {
        demo.server.overload(6).done(function (val) {
            equal(val, 6, "Successful return value from server via send");
            end();
        });
    }).fail(function (reason) {
        ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});