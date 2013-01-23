/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

QUnit.module("Connection Facts");

QUnit.test("Default Connection Parameters", function () {
    var con = $.connection;
    QUnit.equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.equal(con.fn.ajaxDataType, "json", "Verifies ajax data type is json.");
    QUnit.equal(con.fn.logging, false, "Verifies logging is disabled.");
    QUnit.equal(con.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
});

QUnit.test("Stopping Connection", function () {
    var con = $.connection;
    // Need to be connected to stop a connection
    QUnit.ok(con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connected), "Verify we can shift from disconnected into connected.");
    QUnit.equal(con.fn.state, con.connectionState.connected, "Verifies connection is now disconnected.");
    QUnit.equal(con.fn.stop(false), con.fn, "Verifying when conneciton is stopped that the same connection is returned.");
    QUnit.equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected after stop has been called.");
});

QUnit.test("Error on send prior to connected state", function () {
    var con = $.connection;
    // Need the con.fn.state to be disconnected to fail on the send
    QUnit.equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.throws(function () { con.fn.send("Something"); }, "Verifying we error on send when disconnected.");
    // Set the connection state to conneting to verify we still error out
    con.changeState(con.fn, con.connectionState.disconnected, con.connectionState.connecting);
    QUnit.throws(function () { con.fn.send("Something"); }, "Verifying we error on send when connecting.");
    // Reset the connection state back to disconnected
    con.changeState(con.fn, con.connectionState.connecting, con.connectionState.disconnected);
});