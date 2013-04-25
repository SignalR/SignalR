/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

QUnit.module("Connection Facts");

QUnit.test("Default Connection Parameters", function () {
    var con = $.connection;
    QUnit.equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.equal(con.fn.ajaxDataType, "json", "Verifies ajax data type is json.");
    QUnit.equal(con.fn.logging, false, "Verifies logging is disabled.");
    QUnit.equal(con.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
});

QUnit.test("Error on send prior to connected state", function () {
    var con = testUtilities.createConnection("foo");
    // Need the con.state to be disconnected to fail on the send
    QUnit.equal(con.state, $.signalR.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.throws(function () { con.send("Something"); }, "Verifying we error on send when disconnected.");
    // Set the connection state to conneting to verify we still error out
    con.state = $.signalR.connectionState.connecting;
    QUnit.throws(function () { con.send("Something"); }, "Verifying we error on send when connecting.");
});