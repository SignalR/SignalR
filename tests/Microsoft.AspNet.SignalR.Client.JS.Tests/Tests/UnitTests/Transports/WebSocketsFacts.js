/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.core.js" />
/// <reference path="..\..\..\SignalR.Client.JS\jquery.signalR.transports.webSockets.js" />

QUnit.module("WebSockets Facts");

QUnit.test("Availability", function () {
    var con = $.connection;
    QUnit.ok(con.transports.webSockets, "Verifies WebSockets transport exists.");
});

QUnit.test("Named Correctly", function () {
    var con = $.connection;
    QUnit.equal(con.transports.webSockets.name, "webSockets", "Verifies WebSockets is named correctly.");
});

QUnit.test("Pinging server with WebSockets uses correct URL", function () {
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
            QUnit.equal(settings.url, connection.url + "/ping", "Connection URL was correct for ping.");
            ajaxCalled = true;
        };

        $.signalR.transports._logic.pingServer(connection);
    } catch (e) {
        QUnit.fail("Something threw when it shouldn't have: " + e.toString());
    } finally {
        $.ajax = savedAjax;
    }

    QUnit.isTrue(ajaxCalled, "Ajax was called.");
});