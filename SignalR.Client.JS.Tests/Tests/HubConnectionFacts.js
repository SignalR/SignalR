/// <reference path="..\QUnit\qunit.js" />
/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

module("Hub Connection Facts");

test("Default Hub Connection Parameters Test", function () {
    var hubCon = $.hubConnection;
    equal(hubCon.fn.state, 4, "Hub Connection should be disconnected.");
    equal(hubCon.fn.ajaxDataType, "json", "Ajax data type should be json.");
    equal(hubCon.fn.logging, false, "Logging should be disabled.");
    equal(hubCon.fn.reconnectDelay, 2000, "Reconnect delay should be 2000 ms.");
    equal(typeof hubCon.fn.qs, "undefined", "qs should not be set");
    equal(typeof hubCon.fn.url, "undefined", "url should not be set");
});