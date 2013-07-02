$.connection.hub.logging = true;
$.connection.hub.connectionSlow(function () {
    log("[connectionSlow]");
});
$.connection.hub.disconnected(function () {
    log("[disconnected]");
});
$.connection.hub.error(function (error) {
    log("[error] " + error);
});
$.connection.hub.received(function (payload) {
    log("[received] " + window.JSON.stringify(payload));
});
$.connection.hub.reconnected(function () {
    log("[reconnected]");
});
$.connection.hub.reconnecting(function () {
    log("[reconnecting]");
});
$.connection.hub.starting(function () {
    log("[starting]");
});
$.connection.hub.stateChanged(function (change) {
    log("[stateChanged] " + displayState(change.oldState) + " => " + displayState(change.newState));
});

// execute this sample
var url = "http://localhost:40476/";
runHubConnectionAPI(url);

function displayState(state) {
    return ["connecting", "connected", "reconnecting", state, "disconnected"][state];
}

function log(data) {
    $("#Messages").append("<li>[" + new Date().toTimeString() + "] " + data + "</li>");
}

function runHubConnectionAPI(url) {
    $.connection.hub.url = url + "signalr";
    $.connection.hubConnectionAPI.client.displayMessage = function (data) {
        log(data);
    }

    $.connection.hub.start().
    done(function () {
        log("transport.name=" + $.connection.hub.transport.name);
        $.connection.hubConnectionAPI.server.displayMessageCaller("Hello Caller!");
        $.connection.hubConnectionAPI.server.joinGroup($.connection.hub.id, "CommonClientGroup").done(function () {
            $.connection.hubConnectionAPI.server.displayMessageGroup("CommonClientGroup", "Hello Group Members!").done(function () {
                $.connection.hubConnectionAPI.server.leaveGroup($.connection.hub.id, "CommonClientGroup").done(function () {
                    $.connection.hubConnectionAPI.server.displayMessageGroup("CommonClientGroup", "Hello Group Members! (caller should not see this message)").done(function () {
                        $.connection.hubConnectionAPI.server.displayMessageCaller("Hello Caller again!");
                    })
                })
            })
        });
    }).
    fail(function (error) {
        log("Failed to connect: " + error);
    });
}

function runBasicAuth() {
    $.connection.hub.url = url + "basicauth/signalr";
    // increase connect timeout to give time to user to input credentials
    $.connection.fn.transportConnectTimeout = 10000;
    $.connection.authHub.client.invoked = function (connectionId, date) {
        log(connectionId);
    }

    $.connection.hub.start().
    done(function () {
        log("transport.name=" + $.connection.hub.transport.name);
        $.connection.authHub.server.invokedFromClient();
    }).
    fail(function (error) {
        log("Failed to connect: " + error);
    });
}
