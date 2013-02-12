QUnit.module("Hub Connection Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + " transport an connect.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(testName);

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            end();
        }).fail(function (reason) {
            assert.ok(false, "Failed to initiate SignalR connection");
            end();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": connection.id and connection.token have new value after stop and start.", testUtilities.defaultTestTimeout * 2, function (end, assert) {
        var connection = testUtilities.createHubConnection(),
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
            }).fail(function (reason) {
                assert.ok(false, "Failed to initiate signalr connection");
                end();
            });

            return deferred.promise();
        };

        run("hello", connection).done(function () {
            oldConnectionID = connection.id;
            oldConnectionToken = connection.token;

            connection.stop();
            setTimeout(function () {
                run("hello2", connection).done(function () {
                    assert.ok(!(oldConnectionID === connection.id), "Connection.id has new value after stop and start");
                    assert.ok(!(oldConnectionToken === connection.token), "Connection.token has new value after stop and start");
                    setTimeout(end, 500);
                });
            })
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });
});