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