// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Doesn't make sense to run these against Azure SignalR, they are already doing redirect responses!
testUtilities.module("Redirect Functional Tests", !window._server.azureSignalR);

QUnit.asyncTimeoutTest("Can connect to endpoint which produces a redirect response", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect", hub: true }),
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

QUnit.asyncTimeoutTest("Original URL used when client stops after a redirect response", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect", hub: true });

    connection.start().done(function () {
        assert.comment("Connection succeeded");
        assert.equal(connection.url, window.location.protocol + "//" + window.location.host + "/signalr/", "The client redirected to the /signalr endpoint.");
        connection.stop();
        assert.equal(connection.url, "redirect", "The client's URL is reset to the initial value after stopping.");
        assert.isNotSet(connection.baseUrl, "The client's URL-related state is reset after stopping.");
        end();
    }).fail(function () {
        assert.fail("Connection failed!");
        end();
    });
});

QUnit.asyncTimeoutTest("Can connect to endpoint which produces a redirect response with a query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string2" -> "/signalr?name1=newValue&name3=value3"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string2", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("name1").then(function (response) {
            assert.equal(response, "newValue", "The client preserved 'name1=newValue' from redirect query string.");
            return hub.server.getQueryStringValue("name3");
        }).then(function (response) {
            assert.equal(response, "value3", "The client preserved 'name3=value3' from redirect query string.");
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

QUnit.asyncTimeoutTest("Does not merge user query string with redirect query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string2" -> "/signalr?name1=newValue&name3=value3"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string2", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub,
        origQs = {
            foo: "bar"
        };

    connection.qs = origQs;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("foo").then(function (response) {
            assert.equal(response, null, "'foo=bar' is absent from connection.qs. In practice, the redirect should reflect the query string.");
            return hub.server.getQueryStringValue("name1");
        }).then(function (response) {
            assert.equal(response, "newValue", "The client preserved 'name1=newValue' from redirect query string.");
            return hub.server.getQueryStringValue("name3");
        }).then(function (response) {
            assert.equal(response, "value3", "The client preserved 'name3=value3' from redirect query string.");
            assert.deepEqual(connection.qs, origQs, "The client did not change the 'qs' property to include pairs from redirect.")
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

QUnit.asyncTimeoutTest("Preserves user query string if last redirect does not include a query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string-clear?foo=bar"
    // -> "/redirect-query-string-clear2?clearedName=clearedValue"
    // -> "/signalr?foo=bar"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string-clear", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub,
        origQs = {
            foo: "bar"
        };

    connection.qs = origQs;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("foo").then(function (response) {
            assert.equal(response, "bar", "The client preserved 'foo=bar' from connection.qs.");
            return hub.server.getQueryStringValue("clearedName");
        }).then(function (response) {
            assert.equal(response, null, "The client did not preserve a query string key-value pair only specified in an intermediate RedirectUrl.");
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

QUnit.asyncTimeoutTest("Clears redirect query string if last redirect does not include a query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string-clear"
    // -> "/redirect-query-string-clear2?clearedName=clearedValue"
    // -> "/signalr"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string-clear", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("clearedName").then(function (response) {
            assert.equal(response, null, "The client did not preserve a query string key-value pair only specified in an intermediate redirect query string.");
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

QUnit.asyncTimeoutTest("Only preserves last redirect query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string"
    // -> "/redirect-query-string2?name1=value1&name2=value2"
    // -> "/signalr?name1=newValue&name3=value3&origName1={context.Request.Query["name1"]}"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("name1").then(function (response) {
            assert.equal(response, "newValue", "The client preserved 'name1=newValue' from redirect query string.");
            return hub.server.getQueryStringValue("name3");
        }).then(function (response) {
            assert.equal(response, "value3", "The client preserved 'name3=value3' from redirect query string.");
            return hub.server.getQueryStringValue("origName1");
        }).then(function (response) {
            assert.equal(response, "value1", "The client preserved 'origName1=value1' from the first redirect query string for the next request in the redirect chain.");
            return hub.server.getQueryStringValue("name2");
        }).then(function (response) {
            assert.equal(response, null, "The client did not preserve a query string key-value pair only specified in an intermediate redirect query string.");
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

QUnit.asyncTimeoutTest("Can connect to endpoint which produces a redirect response with an invalid query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string-invalid" -> "/signalr?redirect=invalid&/?=/&"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string-invalid", hub: true }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        hub.server.getQueryStringValue("redirect").then(function (response) {
            assert.equal(response, "invalid", "The client preserved 'redirect=invalid' from invalid redirect query string.");
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

QUnit.asyncTimeoutTest("Original query string used when client stops after redirect response with a query string", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    // "/redirect-query-string2" -> "/signalr?name1=newValue&name3=value3"
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-query-string2", hub: true });

    connection.start().done(function () {
        assert.comment("Connection succeeded");

        assert.ok(connection._.redirectQs.includes("name1=newValue"), "The client preserved 'name1=newValue' from redirect query string.");
        assert.ok(connection._.redirectQs.includes("name3=value3"), connection._.redirectQs, "The client preserved 'name3=value3' from redirect query string.");

        connection.stop();

        assert.equal(connection._.redirectQs, null, "The client cleared the redirect query string.");

        end();
    }).fail(function () {
        assert.fail("Connection failed!");
        end();
    });
});

QUnit.asyncTimeoutTest("Simulated old client fails when redirect result provided", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-loop", ignoreErrors: true, hub: true, wrapStart: false }),
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
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-loop", ignoreErrors: true, hub: true, wrapStart: false }),
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

QUnit.asyncTimeoutTest("Does not follow redirect url if ProtocolVersion is less than 2.0", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-old-proto", ignoreErrors: true, hub: true, wrapStart: false }),
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

QUnit.asyncTimeoutTest("Does not follow redirect url if ProtocolVersion is greater than 2.1", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-future-proto", ignoreErrors: true, hub: true, wrapStart: false }),
        proxies = connection.createHubProxies(),
        hub = proxies.redirectTestHub;

    connection.start().done(function () {
        assert.fail("Connection should fail!");
        end();
    }).fail(function (e) {
        assert.equal(e.message, "You are using a version of the client that isn't compatible with the server. Client version 2.1, server version 2.2.", "Connection ignored RedirectUrl from new ProtocolVersion response.");
        end();
    });
})

QUnit.asyncTimeoutTest("Does follow redirect url if ProtocolVersion is 2.1", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect-new-proto", hub: true }),
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

testUtilities.runWithAllTransports(function (transport) {
    if (transport === "foreverFrame") {
        // Forever Frame does not support connections that require a Bearer token to connect.
        return;
    }

    QUnit.asyncTimeoutTest(transport + " transport forwards access token provided by redirect response", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createTestConnection(testName, end, assert, { url: "redirect", hub: true }),
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
