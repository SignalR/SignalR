/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.webSockets.js" />

QUnit.module("Web Sockets Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.webSockets, "Verifies Web Sockets transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.webSockets.name, "webSockets", "Verifies Web Sockets is named correctly.");
});