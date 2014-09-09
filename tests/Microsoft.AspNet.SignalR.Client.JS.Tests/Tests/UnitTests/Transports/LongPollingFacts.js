/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.longPolling.js" />

QUnit.module("Long Polling Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.longPolling, "Verifies Long Polling transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.longPolling.name, "longPolling", "Verifies Long Polling is named correctly.");
});

QUnit.test("Does not support keep alives", function () {
    var con = $.connection;
    QUnit.isFalse(con.transports.longPolling.supportsKeepAlive(con));
});