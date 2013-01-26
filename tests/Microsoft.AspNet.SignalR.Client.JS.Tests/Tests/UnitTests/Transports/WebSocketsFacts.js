/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.webSockets.js" />

QUnit.module("WebSockets Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.webSockets, "Verifies WebSockets transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.webSockets.name, "webSockets", "Verifies WebSockets is named correctly.");
});