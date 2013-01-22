QUnit.module("Transports Common - Ajax Send Facts", testUtilities.longPollingEnabled);

QUnit.asyncTimeoutTest("Long Polling transport can trigger method on server via ajaxSend.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo;

    // Must subscribe to at least one method on client
    demo.client.foo = function () { };

    connection.received(function (result) {
        if (result.R === 6) {
            assert.ok(true, "Result successfully received from server via ajaxSend");
        }
        else {
            assert.ok(false, "Invalid result returned from server via ajaxSend");
        }

        end();
    });

    connection.start({ transport: "longPolling" }).done(function () {
        var data = {
            H: "demo",
            M: "Overload",
            A: [6],
            I: 1
        };

        $.signalR.transports._logic.ajaxSend(connection,JSON.stringify(data));
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ajax Send Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Forever Frame transport can trigger method on server via ajaxSend.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo;

    // Must subscribe to at least one method on client
    demo.client.foo = function () { };

    connection.received(function (result) {
        if (result.R === 6) {
            assert.ok(true, "Result successfully received from server via ajaxSend");
        }
        else {
            assert.ok(false, "Invalid result returned from server via ajaxSend");
        }

        end();
    });

    connection.start({ transport: "foreverFrame" }).done(function () {
        var data = {
            H: "demo",
            M: "Overload",
            A: [6],
            I: 1
        };

        $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Transports Common - Ajax Send Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport can trigger method on server via ajaxSend.", testUtilities.defaultTestTimeout, function (end, assert) {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo;

    // Must subscribe to at least one method on client
    demo.client.foo = function () { };

    connection.received(function (result) {
        if (result.R === 6) {
            assert.ok(true, "Result successfully received from server via ajaxSend");
        }
        else {
            assert.ok(false, "Invalid result returned from server via ajaxSend");
        }

        end();
    });

    connection.start({ transport: "serverSentEvents" }).done(function () {
        var data = {
            H: "demo",
            M: "Overload",
            A: [6],
            I: 1
        };

        $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
    }).fail(function (reason) {
        assert.ok(false, "Failed to initiate signalr connection");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

// Web Sockets uses a duplex stream for sending content, thus does not use the ajax methods