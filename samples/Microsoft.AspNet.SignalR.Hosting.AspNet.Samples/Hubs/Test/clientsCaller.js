$(function () {
    var hubConnection = $.connection.hub,
        testHub = $.connection.stressHub,
        messages = [],
        messagesLimit = 5000;
    
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
        common.incrementLabel("#ReceivedLabel");
        if (messages[message] === undefined) {
            messages[message] = 1;
        } else {
            messages[message]++;
            common.writeError("Message " + message + " has been received " + messages[message] + " times");
        }
        if (message < messagesLimit) {
            testHub.server.echoToCaller(common.incrementLabel("#SendLabel"));
        }
    };

    hubConnection.start({ transport: activeTransport })
        .done(function () {
            testHub.server.echoToCaller(common.incrementLabel("#SendLabel"));
        })
        .fail(function (error) {
            common.writeError(error);
        });
});