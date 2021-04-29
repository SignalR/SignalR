// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

testUtilities.module("Connection State Facts");

QUnit.test("Connection state is disconnected in disconnected callback.", function (assert) {
    var connection = testUtilities.createHubConnection(function () { }, assert, "", undefined, false),
        triggered = false;

    connection.disconnected(function () {
        assert.equal(connection.state, $.signalR.connectionState.disconnected, "Connection state is disconnected in disconnected callback.");
        triggered = true;
    });

    connection.start();
    connection.stop();

    assert.isTrue(triggered, "Disconnected handler triggered.");
});

QUnit.test("Connection State Values", function (assert) {
    var con = $.connection;
    assert.equal(con.connectionState.connecting, 0, "Verifies connecting state is 0.");
    assert.equal(con.connectionState.connected, 1, "Verifies connected state is 1.");
    assert.equal(con.connectionState.reconnecting, 2, "Verifies reconnecting state is 2.");
    assert.equal(con.connectionState.disconnected, 4, "Verifies disconnected state is 4.");
});

QUnit.test("Changing State", function (assert) {
    var con = testUtilities.createHubConnection(),
        signalR = $.signalR;

    assert.equal(signalR.changeState(con, signalR.connectionState.disconnected, signalR.connectionState.connecting), true, "Changes state from disconnected to connecting.");
    assert.equal(signalR.changeState(con, signalR.connectionState.connected, signalR.connectionState.reconnecting), false, "Changing state from connected to reconnecting when state is connecting.");

    con.stateChanged(function (change) {
        assert.equal(change.oldState, signalR.connectionState.connecting, "Verifies that the proper old state is passed to the stateChanged event handler.");
        assert.equal(change.newState, signalR.connectionState.connected, "Verifies that the proper new state is passed to the stateChanged event handler.");
    });

    signalR.changeState(con, signalR.connectionState.connecting, signalR.connectionState.connected);
});

QUnit.test("lastError set when error occurrs", function (assert) {
    var connection = testUtilities.createHubConnection();
    $(connection).triggerHandler($.signalR.events.onError, $.signalR._.error("foo", "TestError"));
    assert.equal(connection.lastError.source, "TestError", "lastError not set");
});

QUnit.test("verifyLastActive fires onError if timeout occurs", function (assert) {
    var connection = testUtilities.createHubConnection();
    connection._.lastActiveAt = new Date(0);
    connection._.keepAliveData.activated = true;
    connection.error(function(err) {
        assert.equal(err.source, "TimeoutException", "Disconnected event has expected close reason");
    });

    $.signalR.transports._logic.verifyLastActive(connection);
});

QUnit.test("markLastMessage updates lastActiveAt", function (assert) {
    var connection = testUtilities.createHubConnection();

    delete connection._.lastMessageAt;
    delete connection._.lastActiveAt;

    $.signalR.transports._logic.markLastMessage(connection);

    assert.isSet(connection._.lastMessageAt);
    assert.isSet(connection._.lastActiveAt);
});

QUnit.test("lastError cleared when connection starts.", function (assert) {
    var connection = testUtilities.createHubConnection(function () { }, assert, "", undefined, false);
    connection.lastError = new Error();
    connection.start();
    assert.equal(connection.lastError, null, "lastError should be cleared on start");
    connection.stop();
});

QUnit.test("lastError not cleared when connection stops.", function (assert) {
    var connection = testUtilities.createHubConnection(function () { }, assert, "", undefined, false),
        error = new Error();
    connection.start();
    connection.lastError = error;
    connection.stop();
    assert.equal(connection.lastError, error, "lastError should not be cleared on stop");
});

QUnit.test("_deferral is cleared when disconnected callbacks are invoked.", function (assert) {
    var connection = testUtilities.createHubConnection(function () { }, assert, "", undefined, false),
        disconnectedCalled = false,
        stateChangedCalled = false;

    connection.disconnected(function () {
        assert.isNotSet(connection._deferral, "_defferal is not set in disconnected callback");
        disconnectedCalled = true;
    });

    connection.stateChanged(function (change) {
        if (change.newState === $.signalR.connectionState.disconnected) {
            assert.isNotSet(connection._deferral, "_defferal is not set in stateChanged callback");
            stateChangedCalled = true;
        }
    });

    connection.start();
    connection.stop();
    assert.isTrue(disconnectedCalled, "disconnected callback was called");
    assert.isTrue(stateChangedCalled, "stateChanged callback was called");
});