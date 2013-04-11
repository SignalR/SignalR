var hubConnection = $.connection.hub;
hubConnection.logging = true;

hubConnection.connectionSlow(function () {
    $("#HubMessages").append("<li style='color:blue;'>connectionSlow:</li>");
});
hubConnection.disconnected(function () {
    $("#HubMessages").append("<li>disconnected:</li>");
});
hubConnection.error(function (error) {
    $("#HubMessages").append("<li style='color:red;'>error: " + error + "</li>");
});
//this method receives data from all hubs in json format, we dont want to see this
//hubConnection.received(function (data) {
//    $("#HubMessages").append("<li>received: " + data + "</li>");
//});
hubConnection.reconnected(function () {
    $("#HubMessages").append("<li>reconnected:</li>");
});
hubConnection.reconnecting(function () {
    $("#HubMessages").append("<li style='color:blue;'>reconnecting:</li>");
});
hubConnection.starting(function () {
    $("#HubMessages").append("<li style='color:blue;'>starting:</li>");
});
hubConnection.stateChanged(function (change) {
    $("#HubMessages").append("<li>stateChanged: " + PrintState(change.oldState) + " => " + PrintState(change.newState) + "</li>");
});

function hubConnectionStartDone() {
    $("#HubMessages").append("<li>Connection started!</li>");
    $("#HubMessages").append("<li>hubConnection.ajaxDataType: " + hubConnection.ajaxDataType + "</li>");
    $("#HubMessages").append("<li>hubConnection.disconnectTimeout: " + hubConnection.disconnectTimeout + "</li>");
    $("#HubMessages").append("<li>hubConnection.id: " + hubConnection.id + "</li>");
    $("#HubMessages").append("<li>hubConnection.logging: " + hubConnection.logging + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveData.activated: " + hubConnection.keepAliveData.activated + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveData.checkInterval: " + hubConnection.keepAliveData.checkInterval + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveData.lastKeepAlive: " + hubConnection.keepAliveData.lastKeepAlive + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveData.timeout: " + hubConnection.keepAliveData.timeout + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveData.timeoutWarning: " + hubConnection.keepAliveData.timeoutWarning + "</li>");
    $("#HubMessages").append("<li>hubConnection.keepAliveWarnAt: " + hubConnection.keepAliveWarnAt + "</li>");
    $("#HubMessages").append("<li>hubConnection.qs: " + hubConnection.qs + "</li>");
    $("#HubMessages").append("<li>hubConnection.reconnectDelay: " + hubConnection.reconnectDelay + "</li>");
    $("#HubMessages").append("<li>hubConnection.state: " + PrintState(hubConnection.state) + "</li>");
    $("#HubMessages").append("<li>hubConnection.token: " + hubConnection.token + "</li>");
    $("#HubMessages").append("<li>hubConnection.transport.name: " + hubConnection.transport.name + "</li>");
    $("#HubMessages").append("<li>hubConnection.url: " + hubConnection.url + "</li>");

    $("#HubMessages").append("<li>testHub.hubName: " + testHub.hubName + "</li>");
}

function hubConnectionStartError() {
    $("#HubMessages").append("<li style='color:red;'>Connection failed:" + error + "</li>");
}

function PrintState(state) {
    if (state == 0)
        return "connecting";
    if (state == 1)
        return "connected";
    if (state == 2)
        return "reconnecting";
    if (state == 4)
        return "disconnected";
}

function incrementLabel(id) {
    var value = parseInt($(id).text()) + 1;
    $(id).text(value);
    return value;
}