QUnit.module("Long Polling Facts", testUtilities.transports.longPolling.enabled);

QUnit.asyncTimeoutTest("Multiple sends that reach browser limit always allow for longPolling poll.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
    var connection = testUtilities.createConnection('/groups', end, assert, testName),
        transport = { transport: "longPolling" },
        handle,
        requests = 15; // Do not increase this value, increasing it will result in reaching the browser URL size limit

    connection.received(function (msg) {
        if (msg === "hello world") {
            assert.comment("Message received.");
            clearTimeout(handle);
            end();
        }
    });

    connection.start(transport).done(function () {
        for (var i = 0; i < requests; i++) {
            connection.send({
                type: 1,
                group: "foo" + i
            });
        }

        // Wait for group add to complete
        setTimeout(function () {
            connection.send({
                type: 3,
                group: "foo0",
                message: "hello world"
            });

            handle = setTimeout(function () {
                assert.fail("Send did not get a response quickly enough.");
                end();
            }, 5000);
        }, 3000);
    });

    // Cleanup
    return function () {
        connection.stop();
    };
});