﻿/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

QUnit.module("Forever Frame Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    ok(con.transports.foreverFrame, "Verifies Forever Frame transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    equal(con.transports.foreverFrame.name, "foreverFrame", "Verifies Forever Frame is named correctly.");
});