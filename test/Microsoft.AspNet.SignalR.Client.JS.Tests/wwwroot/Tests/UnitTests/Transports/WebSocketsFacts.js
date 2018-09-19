// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.webSockets.js" />

testUtilities.module("WebSockets Facts");

QUnit.test("Availability", function (assert) {
    var con = $.connection;
    assert.ok(con.transports.webSockets, "Verifies WebSockets transport exists.");
});

QUnit.test("Named Correctly", function (assert) {
    var con = $.connection;
    assert.equal(con.transports.webSockets.name, "webSockets", "Verifies WebSockets is named correctly.");
});

QUnit.test("Pinging server with WebSockets uses correct URL", function (assert) {
    var savedAjax = $.ajax,
        connection = {
            baseUrl: "foo",
            appRelativeUrl: "bar",
            url: "correct",
            transport: "webSockets"
        },
        ajaxCalled = false;

    try {
        $.ajax = function (settings) {
            assert.equal(settings.url, connection.url + "/ping", "Connection URL was correct for ping.");
            ajaxCalled = true;
        };

        $.signalR.transports._logic.pingServer(connection);
    } catch (e) {
        assert.fail("Something threw when it shouldn't have: " + e.toString());
    } finally {
        $.ajax = savedAjax;
    }

    assert.isTrue(ajaxCalled, "Ajax was called.");
});