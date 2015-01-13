QUnit.module("ForeverFrame Facts", testUtilities.transports.foreverFrame.enabled);

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
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        savedVerifyLastActive = $.signalR.transports._logic.verifyLastActive,
        savedParse = connection._parseResponse;

    connection._parseResponse = function (response) {
        $.network.disconnect();
        return savedParse.call(connection, response);
    }

    $.signalR.transports._logic.verifyLastActive = function () {
        assert.fail("verifyLastActive should not be called.");
        end();
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
