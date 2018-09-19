// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.serverSentEvents.js" />

testUtilities.module("Server Sent Events Facts");

QUnit.test("Availability", function (assert) {
    var con = $.connection;
    assert.ok(con.transports.serverSentEvents, "Verifies Server Sent Events transport exists.");
});

QUnit.test("Named Correctly", function (assert) {
    var con = $.connection;
    assert.equal(con.transports.serverSentEvents.name, "serverSentEvents", "Verifies Server Sent Events is named correctly.");
});