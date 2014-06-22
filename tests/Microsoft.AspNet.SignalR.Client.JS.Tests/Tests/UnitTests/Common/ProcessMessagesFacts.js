QUnit.module("Transports Common - Process Messages Facts");

QUnit.test("tryInitialize is triggered on an initialize message.", function () {
    var connection = testUtilities.createConnection(),
        tryInitializeTriggered = false,
        tryInitialize = $.signalR.transports._logic.tryInitialize;

    $.signalR.transports._logic.tryInitialize = function () {
        tryInitializeTriggered = true;
    };

    try {
        $.signalR.transports._logic.processMessages(connection, {
            C: 1234,
            M: [],
            L: 1337,
            G: "foo",
            S: 1
        });

        QUnit.isTrue(tryInitializeTriggered, "tryInitialize was triggered from initialize message");
    } finally {
        $.signalR.transports._logic.tryInitialize = tryInitialize;
    }
});

QUnit.test("Updates keep alive data on any message retrieval.", function () {
    var connection = testUtilities.createConnection(),
        response = {
            C: 1234,
            M: [{ uno: 1, dos: 2 }, { tres: 3, quatro: 4 }],
            L: 1337,
            G: "foo"
        },
        lastMessageTimeStamp = 0;

    // Ensure the connection can utilize the keep alive features
    connection._.keepAliveData = {
        activated: true
    };

    connection._.lastMessageAt = 0;

    connection.transport = {
        supportsKeepAlive: true
    };

    // No message, should noop but still update time stamp
    $.signalR.transports._logic.processMessages(connection);
    QUnit.ok(connection._.lastMessageAt !== lastMessageTimeStamp, "Sent null data, the last message time (" + connection._.lastMessageAt + ") should be different than " + lastMessageTimeStamp);

    // Subtract 1 so there's at least a 1 millisecond time difference
    lastMessageTimeStamp = connection._.lastMessageAt - 1;
    $.signalR.transports._logic.processMessages(connection, response);
    QUnit.ok(connection._.lastMessageAt !== lastMessageTimeStamp, "Sent valid data, the last message time (" + connection._.lastMessageAt + ") should be different than " + lastMessageTimeStamp);
});

QUnit.test("Noop's on keep alive", function () {
    var connection = testUtilities.createConnection(),
        savedMaximizePersistentResponse = $.signalR.transports._logic.maximizePersistentResponse,
        failed = false;

    connection.transport = "foo";

    $.signalR.transports._logic.maximizePersistentResponse = function () {
        QUnit.ok(false, "Tried to maximize a message on keep alive.");
        failed = true;
    };

    $.signalR.transports._logic.processMessages(connection);

    if (!failed) {
        QUnit.ok(true, "Noop'd on null data (equivalent to keep alive).");
    }

    $.signalR.transports._logic.maximizePersistentResponse = savedMaximizePersistentResponse;
});

QUnit.test("Updates group on message retrieval.", function () {
    var connection = testUtilities.createConnection(),
        response = {
            C: 1234,
            M: [{ uno: 1, dos: 2 }, { tres: 3, quatro: 4 }],
            L: 1337,
            G: "foo"
        },
        updateGroupsCount = 0,
        savedUpdateGroups = $.signalR.transports._logic.updateGroups;

    connection.transport = {};

    $.signalR.transports._logic.updateGroups = function () {
        updateGroupsCount++;
    };

    $.signalR.transports._logic.processMessages(connection, response);
    QUnit.ok(updateGroupsCount === 1, "Update groups is called with generic message.");

    $.signalR.transports._logic.processMessages(connection);
    QUnit.ok(updateGroupsCount === 1, "Update groups is not called when we have a keep alive.");

    $.signalR.transports._logic.updateGroups = savedUpdateGroups;
});

QUnit.test("Triggers received handler for each message.", function () {
    var connection = testUtilities.createHubConnection(),
        demo = connection.createHubProxies().demo,
        response = {
            C: 1234,
            M: [{ H: "demo", M: "foo", A: [], S: { value: 555 } }, { H: "demo", M: "foo", A: [], S: { value: 782 } }],
            L: 1337,
            G: "foo"
        },
        accruedValue = 0;

    connection.transport = {};
    demo.client.foo = function () { };

    connection.received(function (data) {
        accruedValue += data.S.value;
    });

    $.signalR.transports._logic.processMessages(connection, response);
    QUnit.ok(accruedValue === 1337, "Received handler is called for each message in queue.");
});

QUnit.test("Message ID is set on connection ID when set.", function () {
    var connection = testUtilities.createConnection(),
        response = {
            M: false,
            L: 1337,
            G: "foo"
        };

    connection.transport = {};

    // No message ID set
    $.signalR.transports._logic.processMessages(connection, response);
    QUnit.isNotSet(connection.messageId, "No message Id is set if there is no message Id in the message.");

    response.C = 1234;
    $.signalR.transports._logic.processMessages(connection, response);
    QUnit.ok(connection.messageId === 1234, "The connection's messageId property is set when a message contains a messageId");
});

QUnit.test("Exceptions thrown in onReceived handlers on not captured.", function () {
    var connection = testUtilities.createConnection(),
        hubConnection = testUtilities.createHubConnection(),
        demo = hubConnection.createHubProxies().demo,
        response = {
            C: 1234,
            M: [{ H: "demo", M: "foo", A: [], S: { value: 555 } }, { H: "demo", M: "foo", A: [], S: { value: 782 } }],
            L: 1337,
            G: "foo"
        },
        error = new Error(),
        hubError = new Error();

    connection.transport = {};
    hubConnection.transport = {};

    connection.received(function () {
        throw error;
    });
    demo.client.foo = function () {
        throw hubError;
    };

    try {
        $.signalR.transports._logic.processMessages(connection, response);
        QUnit.ok(false, "Error in onReceived handlers shouldn't be caught");
    } catch (e) {
        QUnit.equal(e, error);
    }

    $(hubConnection).triggerHandler($.signalR.events.onStarting);

    try {
        $.signalR.transports._logic.processMessages(hubConnection, response);
        QUnit.ok(false, "Error in onReceived handlers shouldn't be caught");
    } catch (e) {
        QUnit.equal(e, hubError);
    }
});