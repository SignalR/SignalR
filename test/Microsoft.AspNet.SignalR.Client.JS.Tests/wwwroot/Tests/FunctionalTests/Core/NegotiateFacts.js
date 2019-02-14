// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("Core - Negotiate Functional Tests");

(function ($, window) {
    var negotiateTester = function (connection, transport, end, assert, testName) {
        var expectedQs = window.encodeURIComponent(testName),
            savedAjax = $.ajax;

        // Verify that the query string parameter was set
        assert.ok(connection.qs.indexOf(expectedQs) >= 0, "Query string contains the test name prior to connection start");

        $.ajax = function (url, settings) {
            if (!settings) {
                settings = url;
                url = settings.url;
            }

            // Check if it's the negotiate request;
            if (url.indexOf("/negotiate") >= 0) {
                // Verify that the query string parameter on the connection is passed via the ajax request
                assert.ok(url.indexOf(expectedQs) >= 0, "Query string parameters were passed in negotiate");
            }

            // Persist the request through to the original ajax request
            return savedAjax.call(this, url, settings);
        };

        connection.start({ transport: transport }).then(end);

        return savedAjax;
    },
        testInvalidProtocol = function (connection, transport, end, assert, protocol) {
            connection.clientProtocol = protocol;

            connection.start({ transport: transport }).done(function () {
                assert.ok(false, "Transport started successfully.");
            }).fail(function () {
                assert.comment("Transport failed to start with incorrect protocol.");
                end();
            });
        };

    testUtilities.runWithAllTransports(function (transport) {
        QUnit.asyncTimeoutTest(transport + " transport negotiate passes query string variables for hub connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { ignoreErrors: true }),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = negotiateTester(connection, transport, end, assert, testName);

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport negotiate passes query string variables for persistent connections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { ignoreErrors: true }),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = negotiateTester(connection, transport, end, assert, testName);

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + " transport negotiate passes client protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { ignoreErrors: true }),
                // Need to ensure that we have a reference to the savedAjax method so we can then reset it in end()
                savedAjax = $.ajax;

            $.ajax = function (url, settings) {
                if (!settings) {
                    settings = url;
                    url = settings.url;
                }

                // Check if it's the negotiate request;
                if (url.indexOf("/negotiate") >= 0) {
                    // Verify that the query string parameter on the connection is passed via the ajax request
                    assert.ok(url.indexOf("clientProtocol=" + connection.clientProtocol) >= 0, "Client protocol passed in negotiate request");
                }

                // Persist the request through to the original ajax request
                return savedAjax.call(this, url, settings);
            };

            connection.start({ transport: transport }).then(end);

            return function () {
                $.ajax = savedAjax;
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + ": connection fails to start with newer protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { wrapStart: false, ignoreErrors: true });

            testInvalidProtocol(connection, transport, end, assert, 1337);

            return function () {
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + ": connection fails to start with older protocol.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { wrapStart: false, ignoreErrors: true });

            testInvalidProtocol(connection, transport, end, assert, .1337);

            return function () {
                connection.stop();
            };
        });

        QUnit.skipIf(window._server.azureSignalR).asyncTimeoutTest(transport + ": connection uses client set transportConnectTimeout.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { wrapStart: false }),
                newTimeout = 4000;

            assert.equal(connection.transportConnectTimeout, 0, "Transport connect timeout is 0 prior to connection start.");
            connection.transportConnectTimeout = newTimeout;

            connection.start().done(function () {
                // Subtract the default timeout from server to get the client side timeout
                assert.equal(connection._.totalTransportConnectTimeout - 5000, newTimeout, "Transport utilized existing transportConnectTimeout instead of using server value.");
                assert.equal(connection.transportConnectTimeout, newTimeout, "connection.transportConnectTimeout was modified.");
                end();
            });

            return function () {
                connection.stop();
            };
        });

        QUnit.asyncTimeoutTest(transport + ": connection fails to start with useful error when connecting to ASP.NET Core", testUtilities.defaultTestTimeout, function (end, assert, testName) {
            var connection = testUtilities.createTestConnection(testName, end, assert, { wrapStart: false, url: "/aspnetcore-signalr", ignoreErrors: true });

            connection.start()
                .done(function () {
                    assert.fail("should have failed to connect");
                    end();
                })
                .catch(function (e) {
                    assert.equal("Detected a connection attempt to an ASP.NET Core SignalR Server. This client only supports connecting to an ASP.NET SignalR Server. See https://aka.ms/signalr-core-differences for details.", e.message);
                    end();
                });

            return function () {
                connection.stop();
            };
        });
    });

    QUnit.asyncTimeoutTest("start reports server error in negotiation response if present", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createTestConnection(testName, end, assert, { wrapStart: false, ignoreErrors: true, url: "/negotiate-error" }),
            newTimeout = 4000;

        connection.start()
            .done(function () {
                assert.fail("connection should have failed to initialize");
                end();
            }).catch(function (e) {
                assert.equal("Error message received from the server: 'Server-provided negotiate error message!'.", e.message);
                end();
            });

        return function () {
            connection.stop();
        };
    });
})($, window);
