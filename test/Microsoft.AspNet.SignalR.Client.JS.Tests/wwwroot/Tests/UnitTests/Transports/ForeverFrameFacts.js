// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

testUtilities.module("Forever Frame Facts");

QUnit.test("Availability", function (assert) {
    var con = $.connection;
    assert.ok(con.transports.foreverFrame, "Verifies Forever Frame transport exists.");
    assert.isSet($.signalR.transports._logic.foreverFrame, "Verifies ForeverFrame maintenance object exists on the common transports object.");
});

QUnit.test("Named Correctly", function (assert) {
    var con = $.connection;
    assert.equal(con.transports.foreverFrame.name, "foreverFrame", "Verifies Forever Frame is named correctly.");
});

QUnit.test("Messages are run through connection JSON parser if set.", function (assert) {
    var called = false,
        connection = $.connection("");

    connection.json = {
        parse: function (text) {
            called = true;
            return window.JSON.parse(text);
        },
        stringify: function (value) {
            return window.JSON.stringify(value);
        }
    };

    $.connection.transports.foreverFrame.receive(connection, { "test": 1 });

    assert.isTrue(called, "Forever Frame uses JSON parser if configured for the connection.");
});

QUnit.test("Messages are not run through connection JSON parser if it's not set.", function (assert) {
    var responseType,
        connection = $.connection("");

    connection._parseResponse = function (response) {
        responseType = typeof(response);
    };

    $.connection.transports.foreverFrame.receive(connection, { "test": 1 });

    assert.equal(responseType, "object", "Forever Frame does not use JSON parser if it's not configured for the connection.");
});

QUnit.test("IFrame is created outside body.", function (assert) {

    if (window.EventSource) {
        assert.ok(true, "test skipped - Forever Frame is not supported browsers with SSE support.");
        return;
    }

    var connection = $.connection("");

    $.connection.transports.foreverFrame.start(connection);

    var frame = $("body")[0].nextSibling;
    assert.equal(frame && frame.tagName, 'IFRAME');

    $.connection.transports.foreverFrame.stop(connection);

    // verify the frame was removed when the transport was stopped
    assert.isTrue($("body")[0].nextSibling === null);
});