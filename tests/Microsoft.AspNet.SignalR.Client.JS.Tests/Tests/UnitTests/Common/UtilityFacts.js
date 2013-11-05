QUnit.module("Transports Common - Utility Facts");

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