$(function () {
    var hubConnection = $.connection.hub;

    hubConnection.logging = true;
    hubConnection.connectionSlow(function () {
        common.writeEvent("connectionSlow");
    });

    hubConnection.disconnected(function () {
        common.writeEvent("disconnected");
    });

    hubConnection.error(function (error) {
        common.writeError("error: " + error);
    });

    hubConnection.reconnected(function () {
        common.writeEvent("reconnected");
    });

    hubConnection.reconnecting(function () {
        common.writeEvent("reconnecting");
    });

    hubConnection.starting(function () {
        common.writeEvent("starting");
    });

    hubConnection.stateChanged(function (change) {
        common.writeEvent("stateChanged: " + common.printState(change.oldState) + " => " + common.printState(change.newState));

        if (change.oldState === $.signalR.connectionState.connecting && change.newState === $.signalR.connectionState.connected) {
            common.writeLine("hubConnection.ajaxDataType: " + hubConnection.ajaxDataType);
            common.writeLine("hubConnection.disconnectTimeout: " + hubConnection.disconnectTimeout);
            common.writeLine("hubConnection.id: " + hubConnection.id);
            common.writeLine("hubConnection.logging: " + hubConnection.logging);
            common.writeLine("hubConnection.keepAliveData.activated: " + hubConnection.keepAliveData.activated);
            common.writeLine("hubConnection.keepAliveData.checkInterval: " + hubConnection.keepAliveData.checkInterval);
            common.writeLine("hubConnection.keepAliveData.lastKeepAlive: " + hubConnection.keepAliveData.lastKeepAlive);
            common.writeLine("hubConnection.keepAliveData.timeout: " + hubConnection.keepAliveData.timeout);
            common.writeLine("hubConnection.keepAliveData.timeoutWarning: " + hubConnection.keepAliveData.timeoutWarning);
            common.writeLine("hubConnection.keepAliveWarnAt: " + hubConnection.keepAliveWarnAt);
            common.writeLine("hubConnection.qs: " + hubConnection.qs);
            common.writeLine("hubConnection.reconnectDelay: " + hubConnection.reconnectDelay);
            common.writeLine("hubConnection.state: " + common.printState(hubConnection.state));
            common.writeLine("hubConnection.token: " + hubConnection.token);
            common.writeLine("hubConnection.transport.name: " + hubConnection.transport.name);
            common.writeLine("hubConnection.url: " + hubConnection.url);
        }
    });
});

var common = (function () {
    var hubMessages = $("#HubMessages");

    return {
        writeEvent: function (line) {
            hubMessages.append("<li style='color:blue;'>" + line + "</li>");
        },
        writeError: function (line) {
            hubMessages.append("<li style='color:red;'>" + line + "</li>");
        },
        writeLine: function (line) {
            hubMessages.append("<li>" + line + "</li>");
        },
        printState: function (state) {
            return ["connecting", "connected", "reconnecting", state, "disconnected"][state];
        },
        incrementLabel : function(label) {
            var value = parseInt(label.text(), 10) + 1;
            label.text(value);
            return value;
        }
    }
})();
