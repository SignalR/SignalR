QUnit.module("Transport Utility Facts");

QUnit.test("getUrl escapes URI characters.", function () {
    var group,
        connection = testUtilities.createHubConnection();

    connection.appRelativeUrl = "/signalr";
    connection.id = "foo";
    // Every unsafe character group name.
    connection.groups = {
        '#$&+,/:;=?@ "<>#%{}|\^[]`': true
    };

    group = $.signalR.transports._logic.getUrl(connection, "webSockets", true);

    QUnit.ok(group.indexOf("&groups=%5B%22%24%26%2B%2C%2F%3A%3B%3D%3F%40%20%5C%22%3C%3E%23%25%7B%7D%7C%5E%5B%5D%60%22%5D") > 0, "Groups are properly encoded on reconnect.");
});