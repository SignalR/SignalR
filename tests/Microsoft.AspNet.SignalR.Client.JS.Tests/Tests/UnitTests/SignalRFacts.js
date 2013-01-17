/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

QUnit.module("SignalR Facts");

QUnit.test("Availability", function () {
    ok($.signalR, "Verifies SignalR is available.");
});

QUnit.test("Connection Availability", function () {
    ok($.connection, "Verifies SignalR connection is available.");
});

QUnit.test("Hub Connection Availability", function () {
    ok($.hubConnection, "Verifies SignalR hub connection is available.");
});

QUnit.test("Version info is available", function () {
    ok($.signalR.version, "Verifies SignalR version is available.");
    ok($.connection.version, "Verifies connection (SignalR) version is available.");
});