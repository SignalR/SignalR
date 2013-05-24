QUnit.module("Hub Method Parameters Facts");

testUtilities.runWithAllTransports(function (transport) {
    QUnit.asyncTimeoutTest(transport + ": call getRequest()", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(string test = 111)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest();
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": call getRequest(\"test\") ", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(string test = test)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest("test");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": call getRequest([\"test\", \"test1\"])", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(params string[] args)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest(["test", "test1"]);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": call getRequest(\"test\", [])", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(string test, params string[] args)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest("test", []);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": call getRequest(\"test\", \"test\")", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(string test, string test1 = , params string[] args)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest("test", "test");
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": call getRequest(\"test\", \"test\", true)", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            paramHub = connection.createHubProxies().paramHub;

        paramHub.client.display = function (value) {
            assert.equal(value, "From getRequest(string test, string test1, bool test2 = true, bool test3 = false)", "Successful called correct Hub method");
            end();
        };

        connection.start({ transport: transport }).done(function () {
            assert.ok(true, "Connected");
            paramHub.server.getRequest("test", "test", true);
        });

        // Cleanup
        return function () {
            connection.stop();
        };
    });

});
