/// <reference path="jquery-1.6.4.js" />
/// <reference path="jquery.signalR.core.js" />
/// <reference path="jquery.signalR.hubs.js" />

module("HubProxy Facts");

test("SignalR Connection Availability", function () {
    ok($.signalR, "Verifies SignalR connection is available.");
});


