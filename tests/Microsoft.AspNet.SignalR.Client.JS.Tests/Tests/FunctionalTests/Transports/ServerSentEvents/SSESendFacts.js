QUnit.module("Server Sent Event Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events can send ", 5000, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
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
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});