QUnit.module("ForeverFrame Facts - Transport is selected appropriately.", testUtilities.transports.foreverFrame.enabled && $.signalR._.ieVersion <= 8);

QUnit.asyncTimeoutTest("foreverFrame transport is not selected when <= ie8 and auto transport.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start().done(function () {
        assert.ok(true, "Connected");

        assert.equal(connection.transport.name, "longPolling", "longPolling transport selected when using auto transport.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("foreverFrame transport is selected when <= ie8 and foreverFrame transport.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName);

    connection.start({ transport: "foreverFrame" }).done(function () {
        assert.ok(true, "Connected");

        assert.equal(connection.transport.name, "foreverFrame", "foreverFrame transport selected when specifying foreverFrame transport.");
        end();
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});