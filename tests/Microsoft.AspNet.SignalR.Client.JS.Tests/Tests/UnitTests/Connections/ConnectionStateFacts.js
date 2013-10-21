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
    var con = $.connection;
    QUnit.equal(con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connecting), true, "Changes state from disconnected to connecting.");
    QUnit.equal(con.changeState(con.fn, con.connectionState.connected, con.connectionState.reconnecting), false, "Changing state from connected to connecting when state is connecting.");

    con.fn.stateChanged(function (change) {
        QUnit.equal(change.oldState, con.connectionState.connecting, "Verifies that the proper old state is passed to the stateChanged event handler.");
        QUnit.equal(change.newState, con.connectionState.connected, "Verifies that the proper new state is passed to the stateChanged event handler.");
        $(this).unbind(con.events.onStateChanged);
    });
    con.changeState(con.fn, con.connectionState.connecting, con.connectionState.connected);

    // Reset the connection state back to disconnected
    con.changeState(con.fn, con.connectionState.connected, con.connectionState.disconnected);
});