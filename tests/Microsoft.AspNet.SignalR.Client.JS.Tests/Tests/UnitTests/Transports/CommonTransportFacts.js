/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

QUnit.module("Common Transport Facts");

QUnit.test("Validate ensureReconnectingState functionality.", function () {
    var connection = testUtilities.createHubConnection(),
        reconnectingCalled = false,
        stateChangedCalled = false;

    connection.reconnecting(function () {
        reconnectingCalled = true;
    });

    connection.stateChanged(function (state) {
        QUnit.equal(state.oldState, $.signalR.connectionState.connected, "State changed called with connected as the old state.");
        QUnit.equal(state.newState, $.signalR.connectionState.reconnecting, "State changed called with reconnecting as the new state.");
        stateChangedCalled = true;
    });

    connection.state = $.signalR.connectionState.connected;

    $.signalR.transports._logic.ensureReconnectingState(connection);

    QUnit.ok(reconnectingCalled, "Reconnecting event handler was called.");
    QUnit.ok(stateChangedCalled, "StateChanged event handler was called.");
    QUnit.equal(connection.state, $.signalR.connectionState.reconnecting, "Connection state is reconnecting.");
});

QUnit.test("Send stringify undefined", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, undefined);

    QUnit.equal(result, undefined, "Undefined value was not treated correctly.");
});

QUnit.test("Send stringify null", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, null);

    QUnit.equal(result, null, "null value was not treated correctly.");
});

QUnit.test("Send stringify doesn't encode a string", function () {
    var signalr = $.connection,
        con = $.connection("test");
    
    var result = signalr.transports._logic.stringifySend(con, "test");

    QUnit.equal(result, "test", "Raw string value was not treated correctly.");
});

QUnit.test("Send stringify encodes an object", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, { test: "test" });

    QUnit.equal(result, "{\"test\":\"test\"}", "Object value was not JSON encoded correctly.");
});

QUnit.test("Send stringify encodes an array", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, [ "test" ]);

    QUnit.equal(result, "[\"test\"]", "Array value was not JSON encoded correctly.");
});