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
