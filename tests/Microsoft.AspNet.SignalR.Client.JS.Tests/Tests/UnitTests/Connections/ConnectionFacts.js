/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

module("Connection Facts");

test("Default Connection Parameters", function () {
    var con = $.connection;
    equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    equal(con.fn.ajaxDataType, "json", "Verifies ajax data type is json.");
    equal(con.fn.logging, false, "Verifies logging is disabled.");
    equal(con.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
});

test("Stopping Connection", function () {
    var con = $.connection;
    // Need to be connected to stop a connection
    ok(con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connected), "Verify we can shift from disconnected into connected.");
    equal(con.fn.state, con.connectionState.connected, "Verifies connection is now disconnected.");
    equal(con.fn.stop(false), con.fn, "Verifying when conneciton is stopped that the same connection is returned.");
    equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected after stop has been called.");
});

test("Error on send prior to connected state", function () {
    var con = $.connection;
    // Need the con.fn.state to be disconnected to fail on the send
    equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    throws(function () { con.fn.send("Something"); }, "Verifying we error on send when disconnected.");
    // Set the connection state to conneting to verify we still error out
    con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connecting);
    throws(function () { con.fn.send("Something"); }, "Verifying we error on send when connecting.");
    // Reset the connection state back to disconnected
    con.changeState(con.fn, con.connectionState.connecting, con.connectionState.disconnected);
});