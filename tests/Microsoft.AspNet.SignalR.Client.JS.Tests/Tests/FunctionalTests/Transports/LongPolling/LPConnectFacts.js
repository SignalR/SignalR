QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Stopping then starting LongPolling connection in error handler does not cause multiple connections.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
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