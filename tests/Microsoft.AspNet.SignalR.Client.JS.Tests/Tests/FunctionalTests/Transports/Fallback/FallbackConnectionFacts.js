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

// 1 test timeout per transport
QUnit.asyncTimeoutTest("Connection times out when initialize not received.", testUtilities.defaultTestTimeout*4, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        savedProcessMessages = $.signalR.transports._logic.processMessages;

    $.signalR.transports._logic.processMessages = function (_, minData) {
        // Look for initialize message, if we get it, ignore it, transports should time out
        if (!minData.S) {
            savedProcessMessages.apply(this, arguments);
        }
    }

    connection.start().done(function () {
        assert.ok(false, "Connection started");
        end();
    }).fail(function () {
        // All transports fell back because they did not get init message, success!
        assert.comment("Connection failed to start!");
        end();
    });

    // Cleanup
    return function () {
        $.signalR.transports._logic.processMessages = savedProcessMessages;
        connection.stop();
    };
});

QUnit.module("Fallback Facts", testUtilities.transports.webSockets.enabled);

QUnit.asyncTimeoutTest("WebSockets fall back to next transport.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);
    var saveWebSocket = window.WebSocket;
    window.WebSocket = function () {
        this.close = $.noop;
    };
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