// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("ForeverFrame Functional Tests", testUtilities.transports.foreverFrame.enabled && !window._server.azureSignalR);

QUnit.asyncTimeoutTest("foreverFrame transport does not throw when it exceeds its iframeClearThreshold while in connecting.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        savedThreshold = $.signalR.transports.foreverFrame.iframeClearThreshold,
        savedReceived = $.signalR.transports.foreverFrame.receive,
        echoHub = connection.createHubProxies().echoHub,
        echoCount = 0,
        start = function () {
            connection.start({ transport: "foreverFrame" }).done(function () {
                if (++echoCount > 2) {
                    assert.comment("No error was thrown via foreverFrame transport.");
                    end();
                }
                else {
                    echoHub.server.echoCallback("hello world");
                }
            });
        };

    echoHub.client.echo = function (msg) {
        connection.stop();
        start();
    };

    // Always clear the dom
    $.signalR.transports.foreverFrame.iframeClearThreshold = 0;

    $.signalR.transports.foreverFrame.receive = function () {
        try {
            savedReceived.apply(this, arguments);
        }
        catch (e) {
            assert.fail("Receive threw.");
            end();
        }
    };

    start();

    // Cleanup
    return function () {
        $.signalR.transports.foreverFrame.iframeClearThreshold = savedThreshold;
        $.signalR.transports.foreverFrame.receive = savedReceived;
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("foreverFrame transport does not trigger verifyLastActive when connection doesn't successfully start.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { hub: true, wrapStart: false, ignoreErrors: true }),
        savedVerifyLastActive = $.signalR.transports._logic.verifyLastActive,
        savedParse = connection._parseResponse;

    connection._parseResponse = function (response) {
        $.network.disconnect();
        return savedParse.call(connection, response);
    }

    $.signalR.transports._logic.verifyLastActive = function (conn) {
        if (conn === connection) {
            assert.fail("verifyLastActive should not be called.");
            end();
            return false;
        } else {
            // Some other test must not have fully cleaned up. Ex failed test output from before testing conn:

            // verifyLastActive should not be called. Expected: true Result: false
            // at QUnit.assert.fail (http://localhost:8989/js/qunit.extensions.js:16:9)
            // at $.signalR.transports._logic.verifyLastActive(http://localhost:8989/Tests/FunctionalTests/Transports/ForeverFrame/FFConnectFacts.js:63:9)
            // at signalR.transports._logic.reconnect(http://localhost:8989/lib/signalr/jquery.signalR.js:1750:17)
            // at signalR.transports.webSockets.reconnect(http://localhost:8989/lib/signalr/jquery.signalR.js:1922:13)
            // at connection.socket.onclose(http://localhost:8989/lib/signalr/jquery.signalR.js:1898:29)
            // at fail (http://localhost:8989/lib/networkmock/jquery.network.mock.js:166:13)
            return savedVerifyLastActive(conn)
        }
    };

    connection.disconnected(function () {
        assert.comment("Connection successfully transitioned to the disconnecting state.");

        // Give time for any unexpected calls to verifyLastActive
        window.setTimeout(end, 1000)
    });

    connection.start({ transport: "foreverFrame" }).done(function () {
        assert.fail("Connection should not be connected.");
        end();
    });

    // Cleanup
    return function () {
        $.signalR.transports._logic.verifyLastActive = savedVerifyLastActive;
        $.network.connect();
        connection.stop();
    };
});
