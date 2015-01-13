QUnit.module("Transports Common - InitHandler Facts");

(function (undefined) {
    function buildFakeConnection() {
        return {
            log: function () { },
            stop: function () {
                this.stopCalled = true;
            },
            _: {},
            stopCalled: false
        };
    }

    function buildFakeTransport() {
        return {
            name: "fake",
            start: function (connection, onSuccess, onFailed) {
                this.onSuccess = onSuccess;
                this.onFailed = onFailed;
            },
            stop: function () {
                this.stopCalled = true;
            },
            stopCalled: false
        };
    }

    QUnit.test("Transport cannot trigger start request after it has already failed.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            ajaxStartCalled = false,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false,
            onFailedResult;

        $.signalR.transports._logic.ajaxStart = function () {
            ajaxStartCalled = true;
        };

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        onFailedResult = fakeTransport.onFailed();
        fakeTransport.onSuccess();

        // Assert
        QUnit.isTrue(onFailedResult, "Transport should stop. onFailed called during initialization.");
        QUnit.isTrue(initOnFallbackCalled, "Transport failure triggered fallback.");
        QUnit.isFalse(initOnSuccessCalled, "Initialization did not complete.");
        QUnit.isFalse(ajaxStartCalled, "Transport failure prevented start request.");
        QUnit.isTrue(fakeTransport.stopCalled, "Transport failure caused the transport to be stopped.");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the connection to be stopped.");

        // Cleanup
        $.signalR.transports._logic.ajaxStart = savedAjaxStart;
    });

    QUnit.test("Transport failure during start request forces the connection to stop.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            ajaxStartCalled = false,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false,
            onFailedResult;

        $.signalR.transports._logic.ajaxStart = function () {
            ajaxStartCalled = true;
        };

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        fakeTransport.onSuccess();
        onFailedResult = fakeTransport.onFailed();

        // Assert
        QUnit.isTrue(onFailedResult, "Transport should stop. onFailed called during initialization.");
        QUnit.isFalse(initOnFallbackCalled, "Transport failure did not trigger fallback.");
        QUnit.isFalse(initOnSuccessCalled, "Initialization did not complete.");
        QUnit.isTrue(ajaxStartCalled, "Transport success triggered start request.");
        QUnit.isTrue(fakeConnection.stopCalled, "Transport failure caused the connection to be stopped.");

        // Cleanup
        $.signalR.transports._logic.ajaxStart = savedAjaxStart;
    });

    QUnit.test("Transport failure after successful start request has no effect.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false,
            onFailedResult;

        $.signalR.transports._logic.ajaxStart = function (connection, onSuccess) {
            onSuccess();
        };

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        fakeTransport.onSuccess();
        onFailedResult = fakeTransport.onFailed();

        // Assert
        QUnit.isFalse(onFailedResult, "Transport should reconnect. onFailed called after initialization.");
        QUnit.isFalse(initOnFallbackCalled, "Transport failure did not trigger fallback.");
        QUnit.isTrue(initOnSuccessCalled, "Initialization completed.");
        QUnit.isFalse(fakeTransport.stopCalled, "Transport failure did not cause the transport to be stopped.");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the connection to be stopped.");

        // Cleanup
        $.signalR.transports._logic.ajaxStart = savedAjaxStart;
    });

    QUnit.test("Transport failure or success after connection stop has no effect.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            ajaxStartCalled = false,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false,
            onFailedResult;

        $.signalR.transports._logic.ajaxStart = function () {
            ajaxStartCalled = true;
        };

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        initHandler.stop();
        fakeTransport.onSuccess();
        onFailedResult = fakeTransport.onFailed();

        // Assert
        QUnit.isTrue(onFailedResult, "Transport should stop. onFailed called after connection stopped.");
        QUnit.isFalse(initOnFallbackCalled, "Transport failure did not trigger fallback.");
        QUnit.isFalse(initOnSuccessCalled, "Initialization did not complete.");
        QUnit.isFalse(ajaxStartCalled, "Transport success did not trigger start request.");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the transport to be stopped.");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the connection to be stopped.");

        // Cleanup
        $.signalR.transports._logic.ajaxStart = savedAjaxStart;
    });

    QUnit.test("A single transport cannot trigger multiple fallbacks.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            initOnFallbackCount = 0,
            transportShouldStop;

        // Act
        initHandler.start(fakeTransport, undefined, function () {
            initOnFallbackCount++;
        });

        // onFailed should return true (meaning the transport should stop) each time in this test
        transportShouldStop = fakeTransport.onFailed();
        transportShouldStop = fakeTransport.onFailed() && transportShouldStop;

        // Assert
        QUnit.isTrue(transportShouldStop, "Transport should stop. onFailed was called during initialization each time.");
        QUnit.equal(initOnFallbackCount, 1, "Multiple transport failures triggered fallback exactly once.");
        QUnit.isTrue(fakeTransport.stopCalled, "Transport failure caused the transport to be stopped");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the connection to be stopped.");
    });

    QUnit.test("Multiple transports can trigger multiple fallbacks.", function () {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport1 = buildFakeTransport(),
            fakeTransport2 = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            initOnFallbackCount = 0,
            transportShouldStop;

        // Act
        initHandler.start(fakeTransport1, undefined, function () {
            initOnFallbackCount++;
        });

        // onFailed should return true (meaning the transport should stop) each time in this test
        transportShouldStop = fakeTransport1.onFailed();

        initHandler.start(fakeTransport2, undefined, function () {
            initOnFallbackCount++;
        });

        transportShouldStop = fakeTransport2.onFailed() && transportShouldStop;

        // Both of the following calls should be ignored
        transportShouldStop = fakeTransport1.onFailed() && transportShouldStop;
        transportShouldStop = fakeTransport2.onFailed() && transportShouldStop;

        // Assert
        QUnit.isTrue(transportShouldStop, "Transports should stop. onFailed was called during initialization each time.");
        QUnit.isTrue(fakeTransport1.stopCalled, "Transport failure caused the first transport to be stopped");
        QUnit.isTrue(fakeTransport2.stopCalled, "Transport failure caused the second transport to be stopped");
        QUnit.equal(initOnFallbackCount, 2, "Each transport triggered fallback once.");
        QUnit.isFalse(fakeConnection.stopCalled, "Transport failure did not cause the connection to be stopped.");
    });

    QUnit.asyncTimeoutTest("Transport timeout can stop transport and trigger fallback.", testUtilities.defaultTestTimeout, function (end, assert) {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            ajaxStartCalled = false,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false;

        $.signalR.transports._logic.ajaxStart = function () {
            ajaxStartCalled = true;
        };

        fakeConnection._.totalTransportConnectTimeout = 100;

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        window.setTimeout(function () {
            fakeTransport.onSuccess();

            // Assert
            QUnit.isTrue(initOnFallbackCalled, "Timeout triggered fallback.");
            assert.isFalse(initOnSuccessCalled, "Initialization did not complete.");
            assert.isFalse(ajaxStartCalled, "Timeout prevented start request.");
            assert.isTrue(fakeTransport.stopCalled, "Timeout caused the transport to be stopped.");
            assert.isFalse(fakeConnection.stopCalled, "Timeout did not cause the connection to be stopped.");
            end();
        }, 200);

        // Cleanup
        return function () {
            $.signalR.transports._logic.ajaxStart = savedAjaxStart;
        };
    });

    QUnit.asyncTimeoutTest("Transport timeout is not triggered after the start request is initiated.", testUtilities.defaultTestTimeout, function (end, assert) {
        // Arrange
        var fakeConnection = buildFakeConnection(),
            fakeTransport = buildFakeTransport(),
            initHandler = $.signalR.transports._logic.initHandler(fakeConnection),
            savedAjaxStart = $.signalR.transports._logic.ajaxStart,
            ajaxStartCalled = false,
            initOnSuccessCalled = false,
            initOnFallbackCalled = false;

        $.signalR.transports._logic.ajaxStart = function () {
            ajaxStartCalled = true;
        };

        fakeConnection._.totalTransportConnectTimeout = 100;

        // Act
        initHandler.start(fakeTransport, function () {
            initOnSuccessCalled = true;
        }, function () {
            initOnFallbackCalled = true;
        });

        fakeTransport.onSuccess();

        window.setTimeout(function () {
            // Assert
            QUnit.isFalse(initOnFallbackCalled, "Timeout did not trigger fallback.");
            assert.isFalse(initOnSuccessCalled, "Initialization did not complete.");
            assert.isTrue(ajaxStartCalled, "Timeout did not prevent start request.");
            assert.isFalse(fakeTransport.stopCalled, "Timeout did not cause the transport to be stopped.");
            assert.isFalse(fakeConnection.stopCalled, "Timeout did not cause the connection to be stopped.");
            end();
        }, 200);

        // Cleanup
        return function () {
            $.signalR.transports._logic.ajaxStart = savedAjaxStart;
        };
    });
})();
