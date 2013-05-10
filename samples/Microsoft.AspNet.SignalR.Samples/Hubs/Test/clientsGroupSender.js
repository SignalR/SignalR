$(function () {
    var hubConnection = $.connection.hub,
        testHub = $.connection.stressHub,
        messages = [],
        messagesLimit = 5000,
        receivedLabel = $("#ReceivedLabel"),
        sendLabel = $("#SendLabel"),
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

    testHub.client.receivedCaller = function (nodeEvent) {
        var message = parseInt(nodeEvent.Data, 10);
        common.incrementLabel(receivedLabel);
        if (message < messagesLimit) {
            testHub.server.echoToGroup(groupName, common.incrementLabel(sendLabel));
        }
    };

    hubConnection.start({ transport: activeTransport })
        .done(function () {
            testHub.server.echoToGroup(groupName, common.incrementLabel(sendLabel));
        })
        .fail(function (error) {
            common.writeError(error);
        });
});