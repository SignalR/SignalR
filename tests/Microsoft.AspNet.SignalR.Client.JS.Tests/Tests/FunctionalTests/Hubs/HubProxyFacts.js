QUnit.module("Hub Proxy Facts", testUtilities.longPollingEnabled);

// This will run successfully once #1442 is completed.  At that point remove the .skip
QUnit.skip.asyncTimeoutTest("Long Polling transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        badHub = connection.createHubProxy('SomeHubThatDoesntExist');

    badHub.on("foo", function () { });

    connection.start({ transport: "longPolling" }).done(function () {
        assert.ok(false, "SignalR connection started with bad hub.");
        end();
    }).fail(function () {
        assert.ok(true, "Success! Failed to initiate SignalR connection with bad hub");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Hub Proxy Facts", testUtilities.foreverFrameEnabled);

QUnit.asyncTimeoutTest("Forever Frame transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        badHub = connection.createHubProxy('SomeHubThatDoesntExist');

    badHub.on("foo", function () { });

    connection.start({ transport: "foreverFrame" }).done(function () {
        assert.ok(false, "SignalR connection started with bad hub.");
        end();
    }).fail(function () {
        assert.ok(true, "Success! Failed to initiate SignalR connection with bad hub");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Hub Proxy Facts", testUtilities.serverSentEventsEnabled);

QUnit.asyncTimeoutTest("Server Sent Events transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        badHub = connection.createHubProxy('SomeHubThatDoesntExist');

    badHub.on("foo", function () { });

    connection.start({ transport: "serverSentEvents" }).done(function () {
        assert.ok(false, "SignalR connection started with bad hub.");
        end();
    }).fail(function () {
        assert.ok(true, "Success! Failed to initiate SignalR connection with bad hub");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.module("Hub Proxy Facts", testUtilities.webSocketsEnabled);

QUnit.asyncTimeoutTest("Web Sockets transport unable to create invalid hub", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(testName),
        badHub = connection.createHubProxy('SomeHubThatDoesntExist');

    badHub.on("foo", function () { });

    connection.start({ transport: "webSockets" }).done(function () {
        assert.ok(false, "SignalR connection started with bad hub.");
        end();
    }).fail(function () {
        assert.ok(true, "Success! Failed to initiate SignalR connection with bad hub");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});