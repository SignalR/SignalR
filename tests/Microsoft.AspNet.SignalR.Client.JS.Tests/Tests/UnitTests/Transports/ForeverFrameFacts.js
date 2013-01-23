/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

QUnit.module("Forever Frame Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.foreverFrame, "Verifies Forever Frame transport exists.");
    QUnit.isSet($.signalR.transports._logic.foreverFrame, "Verifies ForeverFrame maintenance object exists on the common transports object.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.foreverFrame.name, "foreverFrame", "Verifies Forever Frame is named correctly.");
});