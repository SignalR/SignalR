// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("Hub Event Handler Functional Tests");

testUtilities.runWithAllTransports(function (transport) {
    // This test is meant for #2187 but also verifies the fix to #2190, #2160 (for all transports).
    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called correct number of times after start and stop.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
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
                }, 200);
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

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler gets called once for each handler.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0,
            handler = function (value) {
                callCount += 1;
                assert.comment("call #" + callCount);
                if(callCount == 2) {
                    end();
                }
            };

        echoHub.on('echo', handler);
        echoHub.on('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.comment("connected");
            echoHub.server.echoCallback("hello");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler can have two different handlers.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0,
            commonHandler = function(value, handlerName) {
                callCount += 1;
                assert.comment("call #" + callCount + " from handler " + handlerName);
                if(callCount == 2) {
                    end();
                }
            },
            handler1 = function (value) {
                commonHandler(value, "handler1");
            },
            handler2 = function (value) {
                commonHandler(value, "handler2");
            };

        echoHub.on('echo', handler1);
        echoHub.on('echo', handler2);

        connection.start({ transport: transport }).done(function () {
            assert.comment("connected");
            echoHub.server.echoCallback("hello");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler added by registerHubProxies can be removed.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0,
            handler = function(value, handlerName) {
                callCount += 1;
                assert.comment("call #" + callCount + " from '.on' handler");
                if(callCount == 1) {
                    end();
                }
            };

        echoHub.client.echo = function() {
            assert.fail("echo should not have been called!");
        }
        echoHub.on('echo', handler);

        connection.start({ transport: transport }).done(function () {
            assert.comment("connected");

            // Remove the registerHubProxy handler
            echoHub.off('echo', echoHub.client.echo);

            // Call the callback, only the '.on' handler should be triggered.
            echoHub.server.echoCallback("hello");

            // Call the callback a second time. Again, only the '.on' handler should be triggered.
            echoHub.server.echoCallback("hello");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler can be removed without affecting other handlers.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0,
            commonHandler = function(value, handlerName) {
                callCount += 1;
                assert.comment("call #" + callCount + " from handler " + handlerName);
                if(callCount == 1) {
                    end();
                }
            },
            handler1 = function (value) {
                commonHandler(value, "handler1");
            },
            handler2 = function (value) {
                commonHandler(value, "handler2");
            };

        echoHub.on('echo', handler1);
        echoHub.on('echo', handler2);

        // Remove only handler 1
        echoHub.off('echo', handler1);

        connection.start({ transport: transport }).done(function () {
            assert.comment("connected");
            echoHub.server.echoCallback("hello");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler using bound method can be removed without affecting other handlers.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            callCount = 0,
            commonHandler = function(value, handlerName) {
                callCount += 1;
                assert.comment("call #" + callCount + " from handler " + handlerName);
                if(callCount == 1) {
                    end();
                }
            },
            handler1 = function (value) {
                commonHandler(value, "handler1");
            },
            handler2 = function (value) {
                commonHandler(value, "handler2");
            };

        handler1 = handler1.bind(this);
        handler2 = handler2.bind(this);

        echoHub.on('echo', handler1);
        echoHub.on('echo', handler2);

        // Remove only handler 1
        echoHub.off('echo', handler1);

        connection.start({ transport: transport }).done(function () {
            assert.comment("connected");
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
            }, 200);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Hub Event Handler can be removed using manual callback identity.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            handler = function (value) {
                assert.ok(false, "Failed to remove handler");
                end();
            },
            identityObject = {};

        // Add the handler and then remove it using a different callback but the same "identity" object
        // It should be removed in this case.
        echoHub.on('echo', handler, identityObject);
        echoHub.off('echo', function() { /* dummy */ }, identityObject);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            echoHub.server.echoCallback("hello");

            setTimeout(function () {
                assert.ok(true, "Handler was not called, success!");
                end();
            }, 200);
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
            }, 200);
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
            }, 200);
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
            }, 200);
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
                        }, 100);
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

QUnit.asyncTimeoutTest("Hub Event Handler added by old registerHubProxies can be removed.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName),
        echoHub = connection.createHubProxies().echoHub,
        handler = function (value) {
            assert.ok(false, "Failed to remove handler");
            end();
        };

    // makeProxyCallback was copied from the SignalR 1.0.0 version of src/Microsoft.AspNet.SignalR.Core/Scripts/hubs.js.
    // As of 2.4.1, the makeProxyCallback function hasn't changed. It must include the comment "// Call the client hub method" for this test to pass.
    // https://github.com/SignalR/SignalR/pull/4329
    function makeProxyCallback(hub, callback) {
        return function () {
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    };

    // Add the handler and then remove it using a different callback but the same "identity" object
    // It should be removed in this case.
    echoHub.on('echo', makeProxyCallback(echoHub, handler));
    echoHub.off('echo', makeProxyCallback(echoHub, handler));

    connection.start().done(function () {
        assert.ok(true, "Connected");
        echoHub.server.echoCallback("hello");

        setTimeout(function () {
            assert.ok(true, "Handler was not called, success!");
            end();
        }, 200);
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});
