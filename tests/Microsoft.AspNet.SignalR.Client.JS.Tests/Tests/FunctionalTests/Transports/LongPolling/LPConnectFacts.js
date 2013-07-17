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