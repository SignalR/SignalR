QUnit.module("WebSockets Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSocket invalid state sends trigger connection error.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName),
        customErrorText = "Ouch!";

    connection.error(function (error) {
        assert.equal(error.message, $.signalR.resources.webSocketsInvalidState, "Web socket transport catches thrown errors from the socket send.");
        assert.equal(error.source.message, customErrorText, "Web socket transport throws correct error message");
        end();
    });

    connection.start({ transport: "webSockets" }).done(function () {
        connection.socket.send = function () {
            throw new Error(customErrorText);
        };

        connection.send("foo");
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});