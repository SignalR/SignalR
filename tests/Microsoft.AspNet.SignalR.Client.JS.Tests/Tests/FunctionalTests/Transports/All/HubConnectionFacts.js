QUnit.module("Hub Connection Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport an connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": connection.id and connection.token have new value after stop and start.", testUtilities.defaultTestTimeout * 2, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            echoHub = connection.createHubProxies().echoHub,
            run,
            oldConnectionID,
            oldConnectionToken;

        run = function (echo, connection) {
            var deferred = $.Deferred();

            echoHub.client.echo = function (value) {
                assert.equal(value, echo, "Successfuly received message " + echo);
                deferred.resolve();
            };

            connection.start({ transport: transport }).done(function () {
                echoHub.server.echoCallback(echo).done(function () {
                    assert.ok(true, "Successfuly called server method");
                });
            });

            return deferred.promise();
        };

        run("hello", connection).done(function () {
            oldConnectionID = connection.id;
            oldConnectionToken = connection.token;

            connection.stop();

            // Because of #1529
            setTimeout(function () {
                run("hello2", connection).done(function () {
                    assert.notEqual(oldConnectionID, connection.id, "Connection.id has new value after stop and start");
                    assert.notEqual(oldConnectionToken, connection.token, "Connection.token has new value after stop and start");
                    end();
                });
            }, 0);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});