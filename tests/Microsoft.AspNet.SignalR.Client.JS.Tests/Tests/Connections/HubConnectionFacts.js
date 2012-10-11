/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

module("Hub Connection Facts");

test("Default Hub Connection Parameters", function () {
    var hubCon = $.hubConnection;
    equal(hubCon.fn.state, 4, "Verifies hub connection is disconnected.");
    equal(hubCon.fn.ajaxDataType, "json", "Verifies ajax data type is json.");
    equal(hubCon.fn.logging, false, "Verifies logging is disabled.");
    equal(hubCon.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
    equal(typeof hubCon.fn.qs, "undefined", "Verifies qs is not set.");
    equal(typeof hubCon.fn.url, "undefined", "Verifies url is not set.");
});