/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

QUnit.module("Connection State Facts");

QUnit.test("Connection state is disconnected in disconnected callback.", function () {
    var connection = testUtilities.createHubConnection(function () { }, QUnit, "", undefined, false),
        triggered = false;

    connection.disconnected(function () {
        QUnit.equal(connection.state, $.signalR.connectionState.disconnected, "Connection state is disconnected in disconnected callback.");
        triggered = true;
    });

    connection.start();
    connection.stop();

    QUnit.isTrue(triggered, "Disconnected handler triggered.");
});

QUnit.test("Connection State Values", function () {
    var con = $.connection;
    QUnit.equal(con.connectionState.connecting, 0, "Verifies connecting state is 0.");
    QUnit.equal(con.connectionState.connected, 1, "Verifies connected state is 1.");
    QUnit.equal(con.connectionState.reconnecting, 2, "Verifies reconnecting state is 2.");
    QUnit.equal(con.connectionState.disconnected, 4, "Verifies disconnected state is 4.");
});

QUnit.test("Changing State", function () {
    var con = testUtilities.createHubConnection(),
        signalR = $.signalR;

    QUnit.equal(signalR.changeState(con, signalR.connectionState.disconnected, signalR.connectionState.connecting), true, "Changes state from disconnected to connecting.");
    QUnit.equal(signalR.changeState(con, signalR.connectionState.connected, signalR.connectionState.reconnecting), false, "Changing state from connected to reconnecting when state is connecting.");

    con.stateChanged(function (change) {
        QUnit.equal(change.oldState, signalR.connectionState.connecting, "Verifies that the proper old state is passed to the stateChanged event handler.");
        QUnit.equal(change.newState, signalR.connectionState.connected, "Verifies that the proper new state is passed to the stateChanged event handler.");
    });

    signalR.changeState(con, signalR.connectionState.connecting, signalR.connectionState.connected);
});

QUnit.test("lastError set when error occurrs", function () {
    var connection = testUtilities.createHubConnection();
    $(connection).triggerHandler($.signalR.events.onError, $.signalR._.error("foo", "TestError"));
    QUnit.equal(connection._.lastError.source, "TestError", "lastError not set");
});

QUnit.test("verifyLastActive sets lastError if timeout occurs", function () {
    var connection = testUtilities.createHubConnection();
    connection._.lastActiveAt = new Date(0);
    $.signalR.transports._logic.verifyLastActive(connection);
    QUnit.equal(connection._.lastError.source, "TimeoutException", "Disconnected event has expected close reason");
});
