// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("ForeverFrame Functional Tests - Transport is selected appropriately.", testUtilities.transports.foreverFrame.enabled && $.signalR._.ieVersion <= 8 && !window._server.azureSignalR);

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
