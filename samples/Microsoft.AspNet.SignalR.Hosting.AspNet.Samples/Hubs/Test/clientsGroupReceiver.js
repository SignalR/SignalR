$(function () {
    var hubConnection = $.connection.hub,
        testHub = $.connection.stressHub,
        messages = [],
        receivedLabel = $("#ReceivedLabel"),
        groupName = "group1";
    
    testHub.client.clientConnected = function (nodeEvent) {
        common.writeLine("clientConnected: " + JSON.stringify(nodeEvent));
    };

    testHub.client.clientDisconnected = function (nodeEvent) {
        common.writeLine("clientDisconnected: " + JSON.stringify(nodeEvent));
    };

    testHub.client.clientReconnected = function (nodeEvent) {
        common.writeLine("clientReconnected: " + JSON.stringify(nodeEvent));
    };

    testHub.client.joinedGroup = function (nodeEvent) {
        common.writeLine("joinedGroup: " + JSON.stringify(nodeEvent));
    };

    testHub.client.receivedGroup = function (nodeEvent) {
        var message = parseInt(nodeEvent.Data, 10);
        common.incrementLabel(receivedLabel);
        if (messages[message] === undefined) {
            messages[message] = 1;
        } else {
            messages[message]++;
            common.writeError("Message " + message + " has been received " + messages[message] + " times");
        }
    };

    hubConnection.start({ transport: activeTransport })
        .done(function () {
            testHub.server.joinGroup(groupName, null);
        })
        .fail(function (error) {
            common.writeError(error);
        });
});