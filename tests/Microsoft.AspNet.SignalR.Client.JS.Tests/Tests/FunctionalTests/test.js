module("Test Facts");

asyncTest("Connection Start and method invocation", function () {
    var connection = $.hubConnection('http://localhost:1337');
    var demo = connection.createHubProxies().demo;

    demo.client.foo = function () {
    }

    $.connection.hub.logging = true;

    connection.start().done(function () {
        demo.server.overload(6).done(function (val) {
            console.log("FROM SERVER: " + val);
            equal(val, 6, "Successful return value from server");
            start();
        });
    });
});