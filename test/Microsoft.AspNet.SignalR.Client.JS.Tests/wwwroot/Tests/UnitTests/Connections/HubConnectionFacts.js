// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

testUtilities.module("Hub Connection Facts");

QUnit.test("Default Hub Connection Parameters", function (assert) {
    var hubCon = $.hubConnection;
    assert.equal(hubCon.fn.state, 4, "Verifies hub connection is disconnected.");
    assert.equal(hubCon.fn.ajaxDataType, "text", "Verifies ajax data type is text.");
    assert.equal(hubCon.fn.logging, false, "Verifies logging is disabled.");
    assert.equal(hubCon.fn.reconnectDelay, 2000, "Verifies reconnect delay is 2000 ms.");
    assert.isNotSet(hubCon.fn.qs, "Verifies qs is not set.");
    assert.isNotSet(hubCon.fn.url, "Verifies url is not set.");
});