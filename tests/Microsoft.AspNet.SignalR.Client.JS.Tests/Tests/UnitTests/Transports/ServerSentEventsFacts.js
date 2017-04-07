﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.serverSentEvents.js" />

QUnit.module("Server Sent Events Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.serverSentEvents, "Verifies Server Sent Events transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.serverSentEvents.name, "serverSentEvents", "Verifies Server Sent Events is named correctly.");
});