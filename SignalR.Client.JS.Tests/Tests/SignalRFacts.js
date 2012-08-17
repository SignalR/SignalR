/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

module("SignalR Facts");

test("SignalR Availability", function () {
    ok($.signalR, "Verifies SignalR is available.");    
});

test("SignalR Connection Availability", function () {
    ok($.connection, "Verifies SignalR connection is available.");
});

test("SignalR Hub Connection Availability", function () {
    ok($.hubConnection, "Verifies SignalR hub connection is available.");
});