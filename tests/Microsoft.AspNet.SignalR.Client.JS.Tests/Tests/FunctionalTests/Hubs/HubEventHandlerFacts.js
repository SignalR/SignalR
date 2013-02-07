QUnit.module("Hub Event Handler Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called.", testUtilities.defaultTestTimeout, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
            echoHub = connection.createHubProxies().echoHub,
            handler;

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
        };

        handler = function (value) {
            assert.ok(true, "Successful call handler");
            end();
        };

        echoHub.on('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello");
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler can be removed.", testUtilities.defaultTestTimeout, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
            echoHub = connection.createHubProxies().echoHub,
            handler;

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
        };

        handler = function (value) {
            assert.ok(false, "Failed to remove handler");
            end();
        };

        echoHub.on('echo', handler);
        echoHub.off('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello");

            setTimeout(function () {
                assert.ok(true, "Handler was not called, success!");
                end();
            }, 2000);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event can be removed.", testUtilities.defaultTestTimeout, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
            echoHub = connection.createHubProxies().echoHub,
            handler;

        echoHub.client.echo = function (value) {
            assert.ok(false, "Failed to remove event");
            end();
        };

        handler = function (value) {
            assert.ok(false, "Failed to remove handler");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.off('echo');
            echoHub.server.echoCallback("hello");

            setTimeout(function () {
                assert.ok(true, "Handler was not called, success!");
                end();
            }, 2000);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    // This will run successfully after #1260 is completed (remove skip once it runs successfully)
    QUnit.skip.asyncTimeoutTest(transport + ": Hub Event Handler can be removed after multiple adds.", testUtilities.defaultTestTimeout, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
            echoHub = connection.createHubProxies().echoHub,
            handler;

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
        };

        handler = function (value) {
            assert.ok(false, "Failed to remove handler");
            end();
        };

        echoHub.on('echo', handler);
        echoHub.on('echo', handler);
        echoHub.on('echo', handler);
        echoHub.off('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello");

            setTimeout(function () {
                assert.ok(true, "Handler was not called, success!");
                end();
            }, 2000);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    // This will run successfully after #1259 is completed (remove skip once it runs successfully)
    QUnit.skip.asyncTimeoutTest(transport + ": Hub Event can be removed after handler added again.", testUtilities.defaultTestTimeout, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
            echoHub = connection.createHubProxies().echoHub,
            handler;

        echoHub.client.echo = function (value) {
            assert.ok(false, "Failed to remove event");
            end();
        };

        handler = function (value) {
            assert.ok(false, "Failed to remove handler");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.off('echo', handler);
            echoHub.off('echo');
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.on('echo', handler);
            echoHub.off('echo', handler);
            echoHub.off('echo');
            echoHub.server.echoCallback("hello");

            setTimeout(function () {
                assert.ok(true, "Handler was not called, success!");
                end();
            }, 2000);
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate signalr connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});
