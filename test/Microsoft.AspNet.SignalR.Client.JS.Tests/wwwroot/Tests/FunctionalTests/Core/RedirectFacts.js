// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Doesn't make sense to run these against Azure SignalR, they are already doing redirect responses!
testUtilities.skipOnAzureModule("Redirect Functional Tests");

QUnit.asyncTimeoutTest("Can connect to endpoint which produces a redirect response", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "/redirect"),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.echoReturn("Test Message").then(function (response) {
            assert.equal(response, "Test Message", "Successfully called a server method");
            end();
        }).fail(function () {
            assert.fail("Invocation failed!");
            end();
        });
    }).fail(function () {
        assert.fail("Connection failed!");
        end();
    });
});

QUnit.asyncTimeoutTest("Simulated old client fails when redirect result provided", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "/redirect-loop", false),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.clientProtocol = "1.5";
    connection.supportedProtocols = ["1.5"];

    connection.start().done(function () {
        assert.fail("Connection should fail!");
        end();
    }).fail(function (e) {
        assert.equal(e.message, "You are using a version of the client that isn't compatible with the server. Client version 1.5, server version 2.0.", "Connection failed due to expected ProtocolVersion mismatch.");
        end();
    });
});

QUnit.asyncTimeoutTest("Limits redirects", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "/redirect-loop", false),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.fail("Connection should fail!");
        end();
    }).fail(function (e) {
        assert.equal(e.source.message, "Negotiate redirection limit exceeded.", "Connection failed due to negotiate redirection limit.");
        end();
    });
});

QUnit.asyncTimeoutTest("Does not follow redirect url if ProtocolVersion is not 2.0", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, "/redirect-old-proto", false),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.fail("Connection should fail!");
        end();
    }).fail(function (e) {
        assert.equal(e.message, "No transport could be initialized successfully. Try specifying a different transport or none at all for auto initialization.", "Connection ignored RedirectUrl on old ProtocolVersion response.");
        end();
    });
});

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport forwards access token provided by redirect response", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, "/redirect"),
            proxies = connection.createHubProxies(),
            hub = proxies.redirectTestHub;

        connection.start({ transport: transport }).done(function () {
            assert.comment("Connection succeeded");

            hub.server.getAccessToken().then(function (header) {
                assert.equal(header, "TestToken", "Successfully authenticated");
                end();
            }).fail(function () {
                assert.fail("Invocation failed!");
                end();
            });
        }).fail(function () {
            assert.fail("Connection failed!");
            end();
        });

        return function () {
            connection.stop();
        };
    });
});
