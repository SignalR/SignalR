QUnit.module("Hub Event Handler Facts");

testUtilities.runWithAllTransports(function (transport) {
    // This test is meant for #2187 but also verifies the fix to #2190, #2160 (for all transports).
    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called correct number of times after start and stop.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName,undefined, false),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0;

        echoHub.client.echo = function () {
            callCount++;
        };

        echoHub.on('echo', function () {
            callCount++;
        });

        connection.start({ transport: transport });
        connection.stop();
        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello").done(function () {
                setTimeout(function () {
                    assert.equal(callCount, 2, "Hub methods added are called only once (one for .on and one for dynamic method).");
                    end();
                }, 2000);
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(true, "Successful call handler");
                end();
            };

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
        };

        echoHub.on('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler can be removed.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(false, "Failed to remove handler");
                end();
            };

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
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
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event can be removed.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(false, "Failed to remove handler");
                end();
            };

        echoHub.client.echo = function (value) {
            assert.ok(false, "Failed to remove event");
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
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    // This will run successfully after #1260 is completed (remove skip once it runs successfully)
    QUnit.skip.asyncTimeoutTest(transport + ": Hub Event Handler can be removed after multiple adds.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(false, "Failed to remove handler");
                end();
            };

        echoHub.client.echo = function (value) {
            assert.equal(value, "hello", "Successful received message");
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
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    // This will run successfully after #1259 is completed (remove skip once it runs successfully)
    QUnit.skip.asyncTimeoutTest(transport + ": Hub Event can be removed after handler added again.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(false, "Failed to remove handler");
                end();
            };

        echoHub.client.echo = function (value) {
            assert.ok(false, "Failed to remove event");
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
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called after stop and start.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            run;

        run = function (echo, connection) {
            var deferred = $.Deferred(),
                complete = false,
                messageCount = 0,
                echoHandler = function (value, from) {
                    messageCount++;
                    assert.equal(value, echo, "Successfuly received message " + echo + " via " + from);

                    if (messageCount > 2) {
                        assert.ok(false, "Event called more times than expected");
                    }

                    if (messageCount === 2) {
                        // Give some time for any extra messages to come through (if any more do come, this test will fail)
                        setTimeout(function () {
                            echoHub.off("echo");
                            deferred.resolve();
                        }, 1000);
                    }
                };

            echoHub.client.echo = function (value) {
                echoHandler(value, "(Dynamic)");
            }

            echoHub.on('echo', function (value) {
                echoHandler(value, "(On)");
            });

            connection.start({ transport: transport }).done(function () {
                echoHub.server.echoCallback(echo).done(function () {
                    assert.ok(true, "Successfuly called server method");
                });
            });

            return deferred.promise();
        };

        run("hello", connection).done(function () {
            connection.stop();
            run("hello2", connection).done(function () {
                end();
            });
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});
