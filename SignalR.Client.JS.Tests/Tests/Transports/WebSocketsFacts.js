/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.webSockets.js" />

module("Web Sockets Facts");

test("Web Sockets Availability", function () {
    var con = $.connection;
    ok(con.transports.webSockets, "Verifies Web Sockets transport exists.");
});

test("Web Sockets Named Correctly", function () {
    var con = $.connection;
    equal(con.transports.webSockets.name, "webSockets", "Verifies Web Sockets is named correctly.");
});