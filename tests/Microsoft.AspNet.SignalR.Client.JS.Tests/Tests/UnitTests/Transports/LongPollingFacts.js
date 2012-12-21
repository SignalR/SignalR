/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.longPolling.js" />

QUnit.module("Long Polling Facts");

QUnit.test("Long Polling Availability", function () {
    var con = $.connection;
    ok(con.transports.longPolling, "Verifies Long Polling transport exists.");
});

QUnit.test("Long Polling Named Correctly", function () {
    var con = $.connection;
    equal(con.transports.longPolling.name, "longPolling", "Verifies Long Polling is named correctly.");
});