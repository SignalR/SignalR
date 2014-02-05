﻿QUnit.module("Transports Common - Utility Facts");

QUnit.test("Validate ensureReconnectingState functionality.", function () {
    var connection = testUtilities.createHubConnection(),
        reconnectingCalled = false,
        stateChangedCalled = false;

    connection.reconnecting(function () {
        reconnectingCalled = true;
    });

    connection.stateChanged(function (state) {
        QUnit.equal(state.oldState, $.signalR.connectionState.connected, "State changed called with connected as the old state.");
        QUnit.equal(state.newState, $.signalR.connectionState.reconnecting, "State changed called with reconnecting as the new state.");
        stateChangedCalled = true;
    });

    connection.state = $.signalR.connectionState.connected;

    $.signalR.transports._logic.ensureReconnectingState(connection);

    QUnit.ok(reconnectingCalled, "Reconnecting event handler was called.");
    QUnit.ok(stateChangedCalled, "StateChanged event handler was called.");
    QUnit.equal(connection.state, $.signalR.connectionState.reconnecting, "Connection state is reconnecting.");
});

QUnit.test("markActive stops connection if called after extended period of time.", function () {
    var connection = testUtilities.createConnection(),
        stopCalled = false;

    connection._.lastActiveAt = new Date(new Date().valueOf() - 3000).getTime()
    connection.reconnectWindow = 2900;

    connection.stop = function () {
        stopCalled = true;
    };

    $.signalR.transports._logic.markActive(connection);

    QUnit.equal(stopCalled, true, "Stop was called.");
});

QUnit.test("Ping XHRs in flight get aborted and deleted on connection stop.", function () {
    var connection = testUtilities.createConnection(),
        savedAjax = $.ajax,
        pingSucceeded = false,
        pingFailed = false;

    $.ajax = function (settings) {
        return {
            abort: function (statusText) {
                settings.error({}, statusText);
                settings.complete();
            },
            resolve: function (result) {
                settings.success(result);
                settings.complete();
            }
        };
    };

    connection.state = $.signalR.connectionState.connected;

    $.signalR.transports._logic.pingServer(connection).done(function () {
        pingSucceeded = true;
    });

    $.signalR.transports._logic.pingServer(connection).fail(function () {
        pingFailed = true;
    });

    $.signalR.transports._logic.pingServer(connection).done(function () {
        QUnit.fail("Last ping should neither succeed or fail, because it should be aborted on stop.");
    }).fail(function () {
        QUnit.fail("Last ping should neither succeed or fail, because it should be aborted on stop.");
    });


    connection._.activePings[0].resolve('{"Response": "pong"}');
    QUnit.isTrue(pingSucceeded, "The first ping should succeed.");
    QUnit.isNotSet(connection._.activePings[0], "The first ping should be deleted after it succeeds.");

    // Without a statusText, the following should not be confused for an abort in stop.
    connection._.activePings[1].abort();
    QUnit.isTrue(pingFailed, "The second ping should fail.");
    QUnit.isNotSet(connection._.activePings[1], "The first second ping should be deleted after it fails.");

    QUnit.isSet(connection._.activePings[2], "The last ping shouldn't complete until stop aborts it.");
    connection.stop();
    QUnit.isNotSet(connection._.activePings[2], "The last ping should be deleted after stop aborts it.")

    $.ajax = savedAjax;
});