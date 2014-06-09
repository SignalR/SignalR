QUnit.module("Transports Common - Url Facts");

QUnit.test("getUrl handles groupsToken correctly.", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "&groupsToken=%24%26%2B%2C%2F%3A%3B%3D%3F%40%20%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60",
        url;

    // Every unsafe character group name.
    connection.groupsToken = '$&+,/:;=?@ "<>#%{}|\^[]`';

    $.each(["webSocket", "serverSentEvents", "foreverFrame"], function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles baseUrl correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url;

    connection.baseUrl = "__HELLO__";

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();
        if (transport === "webSockets") {
            return false;
        }

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf("__HELLO__") >= 0, connection.baseUrl + " was represented in the Url for " + transport);
    });

    url = $.signalR.transports._logic.getUrl(connection, "webSockets", true);
    QUnit.ok(url.indexOf("__HELLO__") < 0, connection.baseUrl + " was not represented in the Url for webSockets");
});

QUnit.test("getUrl handles appRelativeUrl correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url,
        expectedResult;

    connection.baseUrl = "__HELLO ";
    connection.appRelativeUrl = "SIGNALR__";
    expectedResult = connection.baseUrl + connection.appRelativeUrl;

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();
        if (transport === "webSockets") {
            return false;
        }

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });

    url = $.signalR.transports._logic.getUrl(connection, "webSockets", true);
    QUnit.ok(url.indexOf(expectedResult) < 0, expectedResult + " was not represented in the Url for webSockets");
    QUnit.ok(url.indexOf(connection.appRelativeUrl) >= 0, connection.appRelativeUrl + " was represented in the Url for webSockets");
});

QUnit.test("getUrl handles transport correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url;

    connection.token = '$&+,/:;=?@ "<>#%{}|\^[]`';

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString(),
            expectedResult = "transport=" + transport;

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles token correctly", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "&connectionToken=%24%26%2B%2C%2F%3A%3B%3D%3F%40%20%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60",
        url;

    // Every unsafe character name.
    connection.token = '$&+,/:;=?@ "<>#%{}|\^[]`';

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles data correctly", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "&connectionData=%24%26%2B%2C%2F%3A%3B%3D%3F%40%20%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60",
        url;

    // Every unsafe character name.
    connection.data = '$&+,/:;=?@ "<>#%{}|\^[]`';

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles messageId correctly", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "&messageId=%24%26%2B%2C%2F%3A%3B%3D%3F%40%20%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60",
        url;

    // Every unsafe character name.
    connection.messageId = '$&+,/:;=?@ "<>#%{}|\^[]`';

    $.each(["webSocket", "serverSentEvents", "foreverFrame"], function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, true);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles appendReconnectUrl parameter correctly", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "/reconnect",
        url;

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, true, false);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl handles reconnecting parameter correctly", function () {
    var connection = testUtilities.createHubConnection(),
        expectedResult = "/connect",
        url;

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, false);
        QUnit.ok(url.indexOf(expectedResult) >= 0, expectedResult + " was represented in the Url for " + transport);
    });
});

QUnit.test("getUrl appends tid correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url;

    $.each(testUtilities.transportNames, function () {
        var transport = this.toString();

        url = $.signalR.transports._logic.getUrl(connection, transport, false);
        QUnit.ok(url.match(/&tid=\d+$/g), "The tid was correctly represented at the end of the Url for " + transport);
    });
});

QUnit.test("getUrl structures URL correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url,
        expectedResult,
        transport;

    connection.baseUrl = "foo.";
    connection.appRelativeUrl = "bar.baz";

    $.each(testUtilities.transportNames, function () {
        transport = this.toString();
        expectedResult = connection.baseUrl + connection.appRelativeUrl + "/connect";

        if (transport === "webSockets") {
            return false;
        }

        url = $.signalR.transports._logic.getUrl(connection, transport, false);
        QUnit.ok(url.indexOf(expectedResult) === 0, expectedResult + " was at the front of the Url for " + transport);

        expectedResult = connection.baseUrl + connection.appRelativeUrl + "/reconnect";
        url = $.signalR.transports._logic.getUrl(connection, transport, true, false);
        QUnit.ok(url.indexOf(expectedResult) === 0, expectedResult + " was at the front of the Url for " + transport);
    });

    expectedResult = connection.appRelativeUrl + "/connect";
    url = $.signalR.transports._logic.getUrl(connection, transport, false);
    QUnit.ok(url.indexOf(expectedResult) === 0, expectedResult + " was at the front of the Url for " + transport);

    expectedResult = connection.appRelativeUrl + "/reconnect";
    url = $.signalR.transports._logic.getUrl(connection, transport, true, false);
    QUnit.ok(url.indexOf(expectedResult) === 0, expectedResult + " was at the front of the Url for " + transport);
});

QUnit.test("addQs handles qs correctly", function () {
    var connection = testUtilities.createHubConnection(),
        url = "foo",
        encodeThis = '$&+,/:;=?@ "<>#%{}|\^[]`';

    QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), url, "The url does not change if there is no connection.qs property.");

    connection.qs = "bar"
    QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), "foo?bar", "When connection.qs is a string it is appended to the url.");

    connection.qs = {
        one: 1,
        three: [1, 2, 3],
        goodluck: encodeThis
    };
    QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), "foo?one=1&three%5B%5D=1&three%5B%5D=2&three%5B%5D=3&goodluck=%24%26%2B%2C%2F%3A%3B%3D%3F%40+%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60", "When connection.qs is an object it is appended to the url as a string based parameter list.");

    connection.qs = "?bar"
    QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), "foo?bar", "When connection.qs is a string and has a question mark in front it does not change.");

    connection.qs = "&bar"
    QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), "foo&bar", "When connection.qs is a string and has an ampersand in front it does not change.");

    // This test will pass when #1568 is fixed.
    if (false) {
        connection.qs = "&bar=" + encodeThis;
        QUnit.equal($.signalR.transports._logic.addQs(url, connection.qs), "foo&bar=&goodluck=%24%26%2B%2C%2F%3A%3B%3D%3F%40+%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60", "When connection.qs is a string the connection.qs characters are encoded.");
    }

    connection.qs = 1337;
    QUnit.throws(function () {
        $.signalR.transports._logic.addQs(url, connection.qs);
    }, "When connection.qs is an integer addQs throws.");

    connection.qs = true;
    QUnit.throws(function () {
        $.signalR.transports._logic.addQs(url, connection.qs);
    }, "When connection.qs is a bool addQs throws.");
});