QUnit.module("Connection Facts");

testUtilities.runWithAllTransports(function (transport) {
   
    QUnit.asyncTimeoutTest(transport + " transport can send and receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 4) {
                assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
                assert.equal(values[2], "OnReceivedAsync1", "Received OnReceivedAsync1");
                assert.equal(values[3], "OnReceivedAsync2", "Received OnReceivedAsync2");
                end();
            }
        });

        connection.error(function (err) {
            assert.ok(false, "Error raised");
            end();
        });

        connection.start({ transport: transport }).done(function () {
            connection.send("test");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport can receive messages on connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            values = [];

        connection.received(function (data) {
            values.push(data);

            if (values.length === 2) {
                assert.equal(values[0], "OnConnectedAsync1", "Received OnConnectedAsync1");
                assert.equal(values[1], "OnConnectedAsync2", "Received OnConnectedAsync2");
                end();
            }
        });

        connection.start({ transport: transport });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + " transport throws an error if protocol version is incorrect", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", $.noop, { ok: $.noop }, testName),
            savedAjax = $.ajax;

        function ajaxReplacement(url, settings) {
            var savedSuccess;

            if (!settings) {
                settings = url;
                url = settings.url;
            }

            // Check if it's the negotiate request
            if (url.indexOf("/negotiate") >= 0) {
                // Let the ajax request finish out
                savedSuccess = settings.success;
                settings.success = function (result) {
                    var res = connection._parseResponse(result);
                    res.ProtocolVersion = "1.1";
                    result = connection.json.stringify(res);
                    savedSuccess.apply(this, arguments);
                }
            }

            // Persist the request through to the original ajax request
            savedAjax.call(this, url, settings);
        };

        $.ajax = ajaxReplacement;
        var errorFired = false;

        connection.start({ transport: transport }).fail(function () {
            assert.ok(errorFired, "Protocol version error thrown");
            end();
        });

        connection.error(function (err) {
            errorFired = true;
            assert.equal(err, "You are using a version of the client that isn't compatible with the server. Client version " + connection.clientProtocol + ", server version 1.1.",
                "Protocol version error message thrown");
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            connection.stop();
        };
    });
});

QUnit.module("Connection Facts", !window.document.commandLineTest);

// Replacing window.onerror will not capture uncaught errors originating from inside an iframe
testUtilities.runWithTransports(["longPolling", "serverSentEvents", "webSockets"], function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport does not capture exceptions thrown in onReceived.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("multisend", end, assert, testName),
            onerror = window.onerror;

        window.onerror = function (errorMessage) {
            assert.ok(errorMessage.match(/onReceived error/));
            end();
            return true;
        }

        connection.received(function (data) {
            throw new Error("onReceived error");
        });

        connection.start({ transport: transport });

        // Cleanup
        return function () {
            window.onerror = onerror;
            connection.stop();
        };
    });

});

