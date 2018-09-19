// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.longPolling.js" />

testUtilities.module("Long Polling Facts");

QUnit.test("Availability", function (assert) {
    var con = $.connection;
    assert.ok(con.transports.longPolling, "Verifies Long Polling transport exists.");
});

QUnit.test("Named Correctly", function (assert) {
    var con = $.connection;
    assert.equal(con.transports.longPolling.name, "longPolling", "Verifies Long Polling is named correctly.");
});

QUnit.test("Does not support keep alives", function (assert) {
    var con = $.connection;
    assert.isFalse(con.transports.longPolling.supportsKeepAlive(con));
});