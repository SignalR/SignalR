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
    var con = testUtilities.createConnection("foo");
    // Need the con.state to be disconnected to fail on the send
    QUnit.equal(con.state, $.signalR.connectionState.disconnected, "Verifies connection is disconnected.");
    QUnit.throws(function () { con.send("Something"); }, "Verifying we error on send when disconnected.");
    // Set the connection state to conneting to verify we still error out
    con.state = $.signalR.connectionState.connecting;
    QUnit.throws(function () { con.send("Something"); }, "Verifying we error on send when connecting.");
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
    var con = $.connection(),
        customJson = {};

    con.json = customJson;

    QUnit.equal(con.json, customJson, "Verifies connection.json is settable to a custom object.");
});

QUnit.test("connection.json is unique on different objects when custom", function () {
    var con1 = $.connection(),
        con2 = $.connection(),
        customJson1 = {},
        customJson2 = {};

    con1.json = customJson1;
    con2.json = customJson2;

    QUnit.equal(con1.json, customJson1, "Verifies connection.json is not shared when set to a custom object.");
    QUnit.equal(con2.json, customJson2, "Verifies connection.json is not shared when set to a custom object.");
});