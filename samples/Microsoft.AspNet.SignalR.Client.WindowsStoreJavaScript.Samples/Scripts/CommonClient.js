var connection = $.connection.hub,
    url = "http://localhost:40476/";

connection.logging = true;
connection.connectionSlow(function () {
    log("[connectionSlow]");
});
connection.disconnected(function () {
    log("[disconnected]");
});
connection.error(function (error) {
    log("[error] " + error);
});
connection.received(function (payload) {
    log("[received] " + window.JSON.stringify(payload));
});
connection.reconnected(function () {
    log("[reconnected]");
});
connection.reconnecting(function () {
    log("[reconnecting]");
});
connection.starting(function () {
    log("[starting]");
});
connection.stateChanged(function (change) {
    log("[stateChanged] " + displayState(change.oldState) + " => " + displayState(change.newState));
});

// execute this sample
runHubConnectionAPI(url);

function displayState(state) {
    return ["connecting", "connected", "reconnecting", state, "disconnected"][state];
}

function log(data) {
    $("#Messages").append("<li>[" + new Date().toTimeString() + "] " + data + "</li>");
}

function runHubConnectionAPI(url) {
    var hub = $.connection.hubConnectionAPI;
    connection.url = url + "signalr";
    hub.client.displayMessage = function (data) {
        log(data);
    }

    connection.start().
    done(function () {
        log("transport.name=" + connection.transport.name);
        hub.server.displayMessageCaller("Hello Caller!");
        hub.server.joinGroup(connection.id, "CommonClientGroup").done(function () {
            hub.server.displayMessageGroup("CommonClientGroup", "Hello Group Members!").done(function () {
                hub.server.leaveGroup(connection.id, "CommonClientGroup").done(function () {
                    hub.server.displayMessageGroup("CommonClientGroup", "Hello Group Members! (caller should not see this message)").done(function () {
                        hub.server.displayMessageCaller("Hello Caller again!");
                    })
                })
            })
        });
    }).
    fail(function (error) {
        log("Failed to connect: " + error);
    });
}
