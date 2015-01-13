/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

QUnit.module("Forever Frame Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.foreverFrame, "Verifies Forever Frame transport exists.");
    QUnit.isSet($.signalR.transports._logic.foreverFrame, "Verifies ForeverFrame maintenance object exists on the common transports object.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.foreverFrame.name, "foreverFrame", "Verifies Forever Frame is named correctly.");
});

QUnit.test("Messages are run through connection JSON parser if set.", function () {
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

    QUnit.isTrue(called, "Forever Frame uses JSON parser if configured for the connection.");
});

QUnit.test("Messages are not run through connection JSON parser if it's not set.", function () {
    var responseType,
        connection = $.connection("");

    connection._parseResponse = function (response) {
        responseType = typeof(response);
    };

    $.connection.transports.foreverFrame.receive(connection, { "test": 1 });

    QUnit.equal(responseType, "object", "Forever Frame does not use JSON parser if it's not configured for the connection.");
});

QUnit.test("IFrame is created outside body.", function () {

    if (window.EventSource) {
        QUnit.ok(true, "test skipped - Forever Frame is not supported browsers with SSE support.");
        return;
    }

    var connection = $.connection("");

    $.connection.transports.foreverFrame.start(connection);

    var frame = $("body")[0].nextSibling;
    QUnit.equal(frame && frame.tagName, 'IFRAME');

    if (frame) {
        frame.parentNode.removeChild(frame);
    };
});