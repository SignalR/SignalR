/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.foreverFrame.js" />

QUnit.module("Common Transport Facts");

QUnit.test("Send stringify undefined", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, undefined);

    QUnit.equal(result, undefined, "Undefined value was not treated correctly.");
});

QUnit.test("Send stringify null", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, null);

    QUnit.equal(result, null, "null value was not treated correctly.");
});

QUnit.test("Send stringify doesn't encode a string", function () {
    var signalr = $.connection,
        con = $.connection("test");
    
    var result = signalr.transports._logic.stringifySend(con, "test");

    QUnit.equal(result, "test", "Raw string value was not treated correctly.");
});

QUnit.test("Send stringify encodes an object", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, { test: "test" });

    QUnit.equal(result, "{\"test\":\"test\"}", "Object value was not JSON encoded correctly.");
});

QUnit.test("Send stringify encodes an array", function () {
    var signalr = $.connection,
        con = $.connection("test");

    var result = signalr.transports._logic.stringifySend(con, [ "test" ]);

    QUnit.equal(result, "[\"test\"]", "Array value was not JSON encoded correctly.");
});