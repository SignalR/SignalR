// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

testUtilities.module("Common Transport Facts");

QUnit.test("Validate ensureReconnectingState functionality.", function (assert) {
    var connection = testUtilities.createHubConnection(),
        reconnectingCalled = false,
        stateChangedCalled = false;

    connection.reconnecting(function () {
        reconnectingCalled = true;
    });

    connection.stateChanged(function (state) {
        assert.equal(state.oldState, $.signalR.connectionState.connected, "State changed called with connected as the old state.");
        assert.equal(state.newState, $.signalR.connectionState.reconnecting, "State changed called with reconnecting as the new state.");
        stateChangedCalled = true;
    });

    connection.state = $.signalR.connectionState.connected;

    $.signalR.transports._logic.ensureReconnectingState(connection);

    assert.ok(reconnectingCalled, "Reconnecting event handler was called.");
    assert.ok(stateChangedCalled, "StateChanged event handler was called.");
    assert.equal(connection.state, $.signalR.connectionState.reconnecting, "Connection state is reconnecting.");
});

QUnit.test("Send stringify undefined", function (assert) {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, undefined);

    assert.equal(result, undefined, "Undefined value was not treated correctly.");
});

QUnit.test("Send stringify null", function (assert) {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, null);

    assert.equal(result, null, "null value was not treated correctly.");
});

QUnit.test("Send stringify doesn't encode a string", function (assert) {
    var signalr = $.connection,
        con = $.connection("test");
    
    var result = signalr.transports._logic.stringifySend(con, "test");

    assert.equal(result, "test", "Raw string value was not treated correctly.");
});

QUnit.test("Send stringify encodes an object", function (assert) {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, { test: "test" });

    assert.equal(result, "{\"test\":\"test\"}", "Object value was not JSON encoded correctly.");
});

QUnit.test("Send stringify encodes an array", function (assert) {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, [ "test" ]);

    assert.equal(result, "[\"test\"]", "Array value was not JSON encoded correctly.");
});