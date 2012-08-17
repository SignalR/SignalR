/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />

module("Connection Facts");

test("Default Connection Parameters", function () {
    var con = $.connection;
    equal(con.fn.state, 4, "Verifies connection is disconnected.");
    equal(con.fn.ajaxDataType, "json", "Verifies ajax data type is json.");
    equal(con.fn.logging, false, "Verifies logging is disabled.");
    equal(con.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
});