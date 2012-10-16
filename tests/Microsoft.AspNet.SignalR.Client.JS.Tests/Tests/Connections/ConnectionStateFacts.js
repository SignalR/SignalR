/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

module("Connection State Facts");

test("Connection State Values", function () {
    var con = $.connection;
    equal(con.connectionState.connecting, 0, "Verifies connecting state is 0.");
    equal(con.connectionState.connected, 1, "Verifies connected state is 1.");
    equal(con.connectionState.reconnecting, 2, "Verifies reconnecting state is 2.");
    equal(con.connectionState.disconnected, 4, "Verifies disconnected state is 4.");
});

test("Changing State", function () {
    var con = $.connection;
    equal(con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connecting), true, "Changes state from disconnected to connecting.");
    equal(con.changeState(con.fn, con.connectionState.connected, con.connectionState.reconnecting), false, "Changing state from connected to connecting when state is connecting.");

    con.fn.stateChanged(function (change) {
        equal(change.oldState, con.connectionState.connecting, "Verifies that the proper old state is passed to the stateChanged event handler.");
        equal(change.newState, con.connectionState.connected, "Verifies that the proper new state is passed to the stateChanged event handler.");
        $(this).unbind(con.events.onStateChanged);
    });
    con.changeState(con.fn, con.connectionState.connecting, con.connectionState.connected);

    // Reset the connection state back to disconnected
    con.changeState(con.fn, con.connectionState.connected, con.connectionState.disconnected);
});