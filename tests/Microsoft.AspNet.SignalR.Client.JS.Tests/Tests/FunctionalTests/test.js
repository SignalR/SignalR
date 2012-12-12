module("Test Functional Fact");

asyncTest("Connection Start and method invocation", function () {
    var demo = $.connection.demo;
    demo.client.foo = function () {
    }

    $.connection.hub.logging = true;

    $.connection.hub.start().done(function () {
        demo.server.overload(6).done(function (val) {
            equal(val, 6, "Successful return value from server");
            start();
        });
    }).fail(function (reason) {
        ok(false, "Failed to initiate signalr connection");
    });
});