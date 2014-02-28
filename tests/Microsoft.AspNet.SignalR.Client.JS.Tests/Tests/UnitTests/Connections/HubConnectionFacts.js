/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

QUnit.module("Hub Connection Facts");

QUnit.test("Default Hub Connection Parameters", function () {
    var hubCon = $.hubConnection;
    QUnit.equal(hubCon.fn.state, 4, "Verifies hub connection is disconnected.");
    QUnit.equal(hubCon.fn.ajaxDataType, "text", "Verifies ajax data type is text.");
    QUnit.equal(hubCon.fn.logging, false, "Verifies logging is disabled.");
    QUnit.equal(hubCon.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
    QUnit.isNotSet(hubCon.fn.qs, "Verifies qs is not set.");
    QUnit.isNotSet(hubCon.fn.url, "Verifies url is not set.");
});