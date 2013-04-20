$(function () {
    var hubConnection = $.connection.hub,
        testHub = $.connection.testHub,
        connectionsList = $("#ConnectionsList"),
        groupsList = $("#GroupsList"),
        connectionText = $("#ConnectionText"),
        groupText = $("#GroupText"),
        messageText = $("#MessageText");
    
    testHub.client.clientConnected = function (nodeEvent) {
        common.writeLine("clientConnected: " + JSON.stringify(nodeEvent));
        connectionsList.append("<option id=" + nodeEvent.Data + ">" + nodeEvent.Data + "</option>");
    };

    testHub.client.clientDisconnected = function (nodeEvent) {
        common.writeLine("clientDisconnected: " + JSON.stringify(nodeEvent));
        $("#ConnectionsList option[id=" + nodeEvent.Data + "]").remove();
    };

    testHub.client.clientReconnected = function (nodeEvent) {
        common.writeLine("clientReconnected: " + JSON.stringify(nodeEvent));
        connectionsList.append("<option id=" + nodeEvent.Data + ">" + nodeEvent.Data + "</option>");
    };

    testHub.client.connectionsAll = function (nodeEvent) {
        common.writeLine("connectionsAll: " + JSON.stringify(nodeEvent));
        $.each(nodeEvent.Data, function(index, value) {
            connectionsList.append("<option id=" + value + ">" + value + "</option>");
        });        
    };

    testHub.client.groupsAll = function (nodeEvent) {
        common.writeLine("groupsAll: " + JSON.stringify(nodeEvent));
        $.each(nodeEvent.Data, function (index, value) {
            groupsList.append("<option id=" + value + ">" + value + "</option>");
        });        
    };

    testHub.client.addedGroup = function (nodeEvent) {
        common.writeLine("addedGroup: " + JSON.stringify(nodeEvent));
        groupsList.append("<option id=" + nodeEvent.Data + ">" + nodeEvent.Data + "</option>");
    };

    testHub.client.joinedGroup = function (nodeEvent) {
        common.writeLine("joinedGroup: " + JSON.stringify(nodeEvent));
        $("#JoinedGroupsList").append("<option id=" + nodeEvent.Data + ">" + nodeEvent.Data + "</option>");
    };

    testHub.client.leftGroup = function (nodeEvent) {
        common.writeLine("leftGroup: " + JSON.stringify(nodeEvent));
        $("#JoinedGroupsList option[id=" + nodeEvent.Data + "]").remove();
    };

    testHub.client.received = function (nodeEvent) {
        common.writeLine("received: " + JSON.stringify(nodeEvent));
        $("#ReceivedTextArea").append(nodeEvent.Data + "\n");
    };

    hubConnection.start({ transport: activeTransport })
        .done(function () {

            $("#JoinGroupButton").click(function () {
                if (isRequired(groupText, "Group")) {
                    return;
                }
                testHub.server.joinGroup(groupText.val(), connectionText.val());
                cleanInputs();
            });

            $("#LeaveGroupButton").click(function () {
                if (isRequired(groupText, "Group")) {
                    return;
                }
                testHub.server.leaveGroup(groupText.val(), connectionText.val());
                cleanInputs();
            });

            $("#SendToAllButton").click(function () {
                if (isRequired(messageText, "Message")) {
                    return;
                }
                testHub.server.sendToAll($("#MessageText").val());
                cleanInputs();
            });

            $("#SendToCallerButton").click(function () {
                if (isRequired(messageText, "Message")) {
                    return;
                }
                testHub.server.sendToCaller(messageText.val());
                cleanInputs();
            });

            $("#SendToClientButton").click(function () {
                if (isRequired(connectionText, "ConnectionId") || isRequired(messageText, "Message")) {
                    return;
                }
                testHub.server.sendToClient(connectionText.val(), messageText.val());
                cleanInputs();
            });

            $("#SendToGroupButton").click(function () {
                if (isRequired(groupText, "Group") || isRequired(messageText, "Message")) {
                    return;
                }
                testHub.server.sendToGroup(groupText.val(), messageText.val());
                cleanInputs();
            });
        })
        .fail(function (error) {
            common.writeError(error);
        });

    function cleanInputs() {
        connectionText.val("");
        groupText.val("");
        messageText.val("");
    }

    function isRequired(id, name) {
        if (id.val() === "") {
            $("#ClientMessages").append("<li>" + name + " is required!</li>");
            return true;
        }
        return false;
    }
});