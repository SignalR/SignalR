QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled && !window.document.commandLineTest);

QUnit.asyncTimeoutTest("Stopping then starting LongPolling connection in error handler does not cause multiple connections.", testUtilities.defaultTestTimeout * 4, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        transport = { transport: "longPolling" },
        savedAjax = $.ajax,
        pollCount = 0;

    function ajaxReplacement(url, settings) {
        if (!settings) {
            settings = url;
            url = settings.url;
        }

        if (url.indexOf("/poll") >= 0) {
            pollCount++;
        }

        // Persist the request through to the original ajax request
        return savedAjax.call(this, url, settings);
    };

    connection.start(transport).done(function () {
        connection.error(function () {
            connection.stop();
            setTimeout(function () {
                connection.start(transport).done(function () {
                    // Set timeout in order to let the next long polling poll initiate
                    setTimeout(function () {
                        $.ajax = ajaxReplacement;

                        setTimeout(function () {
                            assert.equal(pollCount, 0, "Long polling transport did not attempt to re-poll");
                            end();
                        }, 2000);
                    }, 500);
                });
            }, 1000);
        });

        window.setTimeout(function () {
            // Triggers reconnect
            connection.pollXhr.abort("foo");
        }, 500);
    });

    // Cleanup
    return function () {
        $.ajax = savedAjax;
        connection.stop();
    };
});

QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Starting and stopping repeatedly doesn't result in multiple active ajax requests.", testUtilities.defaultTestTimeout * 3, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        transport = { transport: "longPolling" },
        savedAjax = $.ajax,
        pollCount = 0,
        repeatCount = 3,
        connectionsStarted = 0;

    function ajaxReplacement(url, settings) {
        if (!settings) {
            settings = url;
            url = settings.url;
        }

        if (url.indexOf("/poll") >= 0) {
            pollCount++;
        }

        // Persist the request through to the original ajax request
        return savedAjax.call(this, url, settings);
    };

    $.ajax = ajaxReplacement;

    for (var i = 0; i < repeatCount; i++) {
        connection.start(transport).done(function () {
            if (connectionsStarted === repeatCount) {
                setTimeout(function () {
                    assert.equal(pollCount, 1, "Poll was only called once (for the init message).");
                    end();
                }, 3000);
            }
        });

        if (++connectionsStarted !== repeatCount) {
            connection.stop();
        }
    }

    // Cleanup
    return function () {
        $.ajax = savedAjax;
        connection.stop();
    };
});

// TODO: Investigate why these tests don't work in phantom.js. Seems like jsonp is borked in phantom
QUnit.module("JSONP Facts", testUtilities.transports.longPolling.enabled && !window.document.commandLineTest);

QUnit.asyncTimeoutTest("JSONP requests are blocked by default in Hubs.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, undefined, false),
        options = { transport: "longPolling", jsonp: true };


    connection.start(options)
              .done(function () {
                  assert.fail("JSONP connection was established successfully");
                  end();
              })
              .fail(function () {
                  assert.comment("JSONP connection failed");
                  end();
              });


    // Cleanup
    return function () {
        connection.stop();
    };
});


QUnit.asyncTimeoutTest("JSONP requests are blocked by default in PersistentConnections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection('/echo', end, assert, testName, false),
        options = { transport: "longPolling", jsonp: true };


    connection.start(options)
              .done(function () {
                  assert.fail("JSONP connection was established successfully");
                  end();
              })
              .fail(function () {
                  assert.comment("JSONP connection failed");
                  end();
              });


    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("JSONP requests worked when enabled in Hubs.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, 'jsonp/signalr', false),
        options = { transport: "longPolling", jsonp: true };


    connection.start(options)
              .done(function () {
                  assert.comment("JSONP connection was established successfully");
                  end();
              })
              .fail(function () {
                  assert.fail("JSONP connection failed");
                  end();
              });


    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("JSONP requests worked when enabled in PersistentConnections.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createConnection('jsonp/echo', end, assert, testName, false),
        options = { transport: "longPolling", jsonp: true };

    connection.start(options)
              .done(function () {
                  assert.comment("JSONP connection was established successfully");
                  end();
              })
              .fail(function () {
                  assert.fail("JSONP connection failed");
                  end();
              });


    // Cleanup
    return function () {
        connection.stop();
    };
});

QUnit.asyncTimeoutTest("Hub methods can be invoked using JSONP.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
    var connection = testUtilities.createHubConnection(end, assert, testName, 'jsonp/signalr'),
        demo = connection.createHubProxies().demo,
        options = { transport: "longPolling", jsonp: true };

    connection.start(options)
              .done(function () {
                  assert.comment("JSONP connection was established successfully");

                  demo.server.overload().done(function () {
                      assert.comment("Successfully invoked demo.server.overload()");
                      end();
                  }).fail(function () {
                      assert.fail("Invoking demo.server.overload() failed");
                      end();
                  });
              });

    // Cleanup
    return function () {
        connection.stop();
    };
});
