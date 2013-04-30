QUnit.module("Fallback Facts");

QUnit.asyncTimeoutTest("Default transports fall back and connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start().done(function () {
        assert.ok(true, "Connected");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Fallback Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSockets fall back to next transport.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);
    var saveWebSocket = window.WebSocket;
    window.WebSocket = function () { };
    connection.start().done(function () {
        assert.notEqual(connection.transport.name, "webSockets", "Connected using " + connection.transport.name);
        end();
    });

    // Cleanup
    return function () {
        window.WebSocket = saveWebSocket;
        connection.stop();
    };
});