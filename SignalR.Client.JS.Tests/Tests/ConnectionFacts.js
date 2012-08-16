/// <reference path="..\QUnit\qunit.js" />
/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.core.js" />

module("Connection Facts");

test("Default Connection Parameters Test", function () {
    var con = $.connection;
    equal(con.fn.state, 4, "Connection should be disconnected.");
    equal(con.fn.ajaxDataType, "json", "Ajax data type should be json.");
    equal(con.fn.logging, false, "Logging should be disabled.");
    equal(con.fn.reconnectDelay, 2000, "Reconnect delay should be 2000 ms.");
});