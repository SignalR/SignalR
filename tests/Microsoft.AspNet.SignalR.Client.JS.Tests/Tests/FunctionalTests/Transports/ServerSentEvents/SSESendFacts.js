QUnit.module("Server Sent Event Facts", testUtilities.transports.serverSentEvents.enabled);

QUnit.asyncTimeoutTest("Can send ", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        proxies = connection.createHubProxies(),
        demo = proxies.demo;

    // Must subscribe to at least one method on client
    demo.client.foo = function () { };

    connection.start({ transport: "serverSentEvents" }).done(function () {
        demo.server.overload(6).done(function (val) {
            assert.equal(val, 6, "Successful return value from server via send");
            end();
        });
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate SignalR connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});