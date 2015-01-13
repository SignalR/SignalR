QUnit.module("WebSockets Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSocket invalid state sends trigger connection error.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection("signalr", end, assert, testName),
        customErrorText = "Ouch!";

    connection.error(function (error) {
        assert.equal(error.message, $.signalR.resources.webSocketsInvalidState, "Web socket transport catches thrown errors from the socket send.");
        assert.isSet(error.source, "Error source is set.")

        // Avoid uncaught TypeError
        if (error.source) {
            assert.equal(error.source.message, customErrorText, "Web socket transport throws correct error message");
        }

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

QUnit.asyncTimeoutTest("Hub invocations fail when the WebSocket in in an invalid state.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        demo = connection.createHubProxies().demo,
        customErrorText = "Ouch!";

    connection.error(function (error) {
        assert.equal(error.message, $.signalR.resources.webSocketsInvalidState, "Web socket transport catches thrown errors from the socket send.");
        assert.isSet(error.source, "Error source is set.")

        // Avoid uncaught TypeError
        if (error.source) {
            assert.equal(error.source.message, customErrorText, "Web socket transport throws correct error message");
        }
    });

    connection.start({ transport: "webSockets" }).done(function () {
        connection.socket.send = function () {
            throw new Error(customErrorText);
        };

        demo.server.getValue().fail(function (e) {
            end();
        });
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("WebSocket transport functions with JSONP enabled.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, 'jsonp/signalr'),
        demo = connection.createHubProxies().demo,
        echoNum = 73;

    connection.start({ transport: "webSockets", jsonp: true }).done(function () {
        demo.server.overload(echoNum).done(function (result) {
            assert.equal(result, echoNum, "Received invocation result");
            end();
        });
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});