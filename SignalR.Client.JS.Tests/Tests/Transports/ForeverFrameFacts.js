/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

module("Forever Frame Facts");

test("Forever Frame Availability", function () {
    var con = $.connection;
    ok(con.transports.foreverFrame, "Verifies Forever Frame transport exists.");
});

test("Forever Frame Correctly", function () {
    var con = $.connection;
    equal(con.transports.foreverFrame.name, "foreverFrame", "Verifies Forever Frame is named correctly.");
});