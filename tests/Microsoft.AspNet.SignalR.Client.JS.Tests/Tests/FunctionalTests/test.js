module("Test Functional Fact");

asyncTest("Connection Start and method invocation", function () {
    var connection = window.createHubConnection(),
        proxies = connection.createHubProxies(),
        demo = proxies.demo;

    demo.client.foo = function () {
    };

    connection.logging = true;

    connection.start().done(function () {
        demo.server.overload(6).done(function (val) {
            equal(val, 6, "Successful return value from server");
            connection.stop();
            start();
        });
    }).fail(function (reason) {
        connection.stop();
        ok(false, "Failed to initiate signalr connection");
        start();
    });
});

asyncTest("Long Polling connection", function () {
    var connection = window.createHubConnection();

    connection.logging = true;

    connection.start({ transport: 'longPolling' }).done(function () {
        ok(true, "Connected");
        connection.stop();
        start();
    }).fail(function (reason) {
        ok(false, "Failed to initiate signalr connection");
        connection.stop();
        start();
    });
});
