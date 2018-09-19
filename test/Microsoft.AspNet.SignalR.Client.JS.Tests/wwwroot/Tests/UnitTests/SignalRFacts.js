// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\SignalR.Client.JS\jquery.signalR.hubs.js" />

testUtilities.module("SignalR Facts");

QUnit.test("Availability", function (assert) {
    assert.ok($.signalR, "Verifies SignalR is available.");
});

QUnit.test("Connection Availability", function (assert) {
    assert.ok($.connection, "Verifies SignalR connection is available.");
});

QUnit.test("Hub Connection Availability", function (assert) {
    assert.ok($.hubConnection, "Verifies SignalR hub connection is available.");
});

QUnit.test("Version info is available", function (assert) {
    assert.ok($.signalR.version, "Verifies SignalR version is available.");
    assert.ok($.connection.version, "Verifies connection (SignalR) version is available.");
});
