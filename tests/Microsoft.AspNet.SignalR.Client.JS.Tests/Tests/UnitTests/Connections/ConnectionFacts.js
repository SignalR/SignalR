/// <reference path="..\..\..\Scripts\_references.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

QUnit.module("Connection Facts");

QUnit.test("Default Connection Parameters", function () {
    var con = $.connection;
    QUnit.equal(con.fn.state, con.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.equal(con.fn.ajaxDataType, "text", "Verifies ajax data type is text.");
    QUnit.equal(con.fn.logging, false, "Verifies logging is disabled.");
    QUnit.equal(con.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
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

QUnit.test("connection.prototype.json is window.JSON", function () {
    var json = $.connection.prototype.json;
    QUnit.equal(json, window.JSON, "Verifies connection.prototype.json is window.JSON.");
});

QUnit.test("connection.json is window.JSON by default", function () {
    var con = $.connection();
    QUnit.equal(con.json, window.JSON, "Verifies connection.json is window.JSON by default.");
});

QUnit.test("connection.json is custom object after set", function () {
    var con = $.connection();
    var customJson = { };
    con.json = customJson;
    QUnit.equal(con.json, customJson, "Verifies connection.json is settable to a custom object.");
});

QUnit.test("pingIntervalId does not change on multiple calls to configurePingInterval", function () {
    var con = testUtilities.createConnection("/signalr", function () { }, QUnit, "", false);

    con._.pingIntervalId = 1;

    $.signalR._.configurePingInterval(con);

    QUnit.equal(con._.pingIntervalId, 1);
});

QUnit.test("lastActiveAt is deleted when a connection stops", function () {
    var con = testUtilities.createConnection("/signalr", function () { }, QUnit, "", false);

    con.start();

    con.stop();
    
    QUnit.isNotSet(con._.lastActiveAt, "lastActiveAt is removed from connection after stop is called");
});