QUnit.module("Ajax Replacement Facts");

testUtilities.runWithTransports(["serverSentEvents", "foreverFrame", "longPolling"], function (transport) {

    QUnit.asyncTimeoutTest(transport + ": Send utilizes signalr defaults correctly.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("signalr", end, assert, testName),
            savedAjax = $.ajax,
            savedAjaxDefaults = $.signalR.ajaxDefaults,
            customAjaxDefaults = {
                processData: 1,
                timeout: 2,
                async: 3,
                global: 4,
                cache: 5
            };

        connection.start({ transport: transport }).done(function () {
            $.signalR.ajaxDefaults = customAjaxDefaults;

            $.ajax = function (url, settings) {
                if (!settings) {
                    settings = url;
                    url = settings.url;
                }

                for (var property in customAjaxDefaults) {
                    assert.deepEqual(settings[property], customAjaxDefaults[property], property + " was correctly persisted to ajax send requests.");
                }

                // Let the stack unwind
                setTimeout(function () {
                    end();
                }, 0);
            };

            connection.send("hello");
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            $.signalR.ajaxDefaults = savedAjaxDefaults;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Connection stop utilizes signalr defaults correctly.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("signalr", end, assert, testName),
            savedAjax = $.ajax,
            savedAjaxDefaults = $.signalR.ajaxDefaults,
            customAjaxDefaults = {
                processData: 1,
                timeout: 2,
                async: 3,
                global: 4,
                cache: 5
            };

        connection.start({ transport: transport }).done(function () {
            $.signalR.ajaxDefaults = customAjaxDefaults;

            $.ajax = function (url, settings) {
                if (!settings) {
                    settings = url;
                    url = settings.url;
                }

                assert.deepEqual(settings.processData, customAjaxDefaults.processData, "processData was correctly persisted to ajax abort requests.");
                assert.notDeepEqual(settings.timeout, customAjaxDefaults.timeout, "timeout was correctly persisted to ajax abort requests.");
                assert.notDeepEqual(settings.async, customAjaxDefaults.async, "async was not persisted to ajax abort requests.");
                assert.deepEqual(settings.processData, customAjaxDefaults.processData, "processData was correctly persisted to ajax abort requests.");

                // Let the stack unwind
                setTimeout(function () {
                    end();
                }, 0);
            };

            connection.stop();
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            $.signalR.ajaxDefaults = savedAjaxDefaults;
            connection.stop();
        };
    });

    QUnit.asyncTimeoutTest(transport + ": Connection overrides default ajax settings (in jquery) to prevent failure.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createHubConnection(end, assert, testName),
            demo = connection.createHubProxies().demo,
            defaultProcessData = $.ajaxSettings.processData,
            defaultTimeout = $.ajaxSettings.timeout,
            defaultAsync = $.ajaxSettings.async;

        $.ajaxSetup({
            processData: false,
            timeout: 1,
            async: false
        });

        connection.received(function (result) {
            if (result.I === "1") {
                if (result.R === 6) {
                    assert.ok(true, "Result successfully received from server via ajaxSend");
                }
                else {
                    assert.ok(false, "Invalid result returned from server via ajaxSend");
                }

                end();
            }
        });

        connection.start({ transport: transport }).done(function () {
            var data = {
                H: "demo",
                M: "Overload",
                A: [6],
                I: 1
            };

            $.signalR.transports._logic.ajaxSend(connection, JSON.stringify(data));
        });

        // Cleanup
        return function () {
            // Replace ajax defaults
            $.ajaxSetup({
                processData: defaultProcessData,
                timeout: defaultTimeout,
                async: defaultAsync
            });

            connection.stop();
        };
    });

});

testUtilities.runWithAllTransports(function (transport) {

    QUnit.asyncTimeoutTest(transport + ": Connection negotiate utilizes signalr defaults correctly.", testUtilities.defaultTestTimeout, function (end, assert, testName) {
        var connection = testUtilities.createConnection("signalr", end, assert, testName),
            savedAjax = $.ajax,
            savedAjaxDefaults = $.signalR.ajaxDefaults,
            customAjaxDefaults = {
                processData: 1,
                timeout: 2,
                async: 3,
                global: 4,
                cache: 5
            };

        $.signalR.ajaxDefaults = customAjaxDefaults;

        $.ajax = function (url, settings) {
            if (!settings) {
                settings = url;
                url = settings.url;
            }

            for (var property in customAjaxDefaults) {
                assert.deepEqual(settings[property], customAjaxDefaults[property], property + " was correctly persisted to ajax negotiate request.");
            }

            $.extend(settings, savedAjaxDefaults);

            $.signalR.ajaxDefaults = savedAjaxDefaults;

            $.ajax = savedAjax;

            return savedAjax.call(this, url, settings);
        };

        connection.start({ transport: transport }).done(function () {
            end();
        });

        // Cleanup
        return function () {
            $.ajax = savedAjax;
            $.signalR.ajaxDefaults = savedAjaxDefaults;
            connection.stop();
        };
    });

});