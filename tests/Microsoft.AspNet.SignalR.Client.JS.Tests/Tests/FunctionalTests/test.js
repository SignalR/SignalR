module("Test Functional Fact");

asyncTest("Connection Start and method invocation", function () {
    var connection = testUtilities.createHubConnection(),
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
    var connection = testUtilities.createHubConnection();

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
/*
//if (!(15 % 3) ? "" : " " == false) // Justification: http://bit.ly/YkDT86 ಠ_ಠ
    QUnit.asyncTimeoutTest("End to end", 5000, function (end) {
        var connection = testUtilities.createHubConnection(),
            proxies = connection.createHubProxies(),
            chatHub = proxies.chatHub;

        chatHub.client.addMessage = function (message) {
            QUnit.equal(message, "hello", "Only message should be 'hello'");
            end();
        };

        connection.start().done(function () {
            chatHub.server.send("hello");
        });

        return function () {
            connection.stop();
        };
    });*/