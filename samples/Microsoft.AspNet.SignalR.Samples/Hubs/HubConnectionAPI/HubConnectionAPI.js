/// <reference path="../../Scripts/jquery-1.8.2.js" />
function writeEvent(line) {
    var messages = $("#messages");
    messages.append("<li style='color:blue;'>" + getTimeString() + ' ' + line + "</li>");
}

function writeError(line) {
    var messages = $("#messages");
    messages.append("<li style='color:red;'>" + getTimeString() + ' ' + line + "</li>");
}

function writeLine(line) {
    var messages = $("#messages");
    messages.append("<li style='color:black;'>" + getTimeString() + ' ' + line + "</li>");
}

function printState(state) {
    return ["connecting", "connected", "reconnecting", state, "disconnected"][state];
}

function getTimeString() {
    var currentTime = new Date();
    return currentTime.toTimeString();
}


$(function () {
    "use strict";

    var hubConnectionAPI = $.connection.hubConnectionAPI,
        messages = $("#messages"),
        groupNameTextInput = $("#group"),
        connectionTextInput = $("#connection"),
        messageTextInput = $("#message"),
        groupMessageTextInput = $("#groupMessage"),
        meTextInput = $("#me"),
        stopStartBtn = $("#stopStart"),
        start;

    var connection = $.connection.hub;
    connection.logging = true;

    connection.connectionSlow(function () {
        writeEvent("connectionSlow id: " + connection.id + " state: " + printState(connection.state) + " transport: " + connection.transport.name);
    });

    connection.disconnected(function () {
        writeEvent("disconnected id: " + connection.id + " state: " + printState(connection.state));
        stopStartBtn.prop("disabled", false)
                    .find("span")
                        .text("Start Connection")
                        .end()
                    .find("i")
                        .removeClass("icon-stop")
                        .addClass("icon-play");
    });

    connection.error(function (error) {
        writeError("Error: " + error);
    });

    connection.reconnected(function () {
        writeEvent("reconnected id: " + connection.id + " state: " + printState(connection.state) + " transport: " + connection.transport.name);
    });

    connection.reconnecting(function () {
        writeEvent("reconnecting id: " + connection.id + " state: " + printState(connection.state) + " transport: " + connection.transport.name);
    });

    connection.starting(function () {
        writeEvent("starting state: " + printState(connection.state));
    });

    connection.stateChanged(function (change) {
        writeEvent("stateChanged: " + printState(change.oldState) + " => " + printState(change.newState));
    });

    hubConnectionAPI.client.displayMessage = function (value) {
        writeLine(value);
    }

    start = function () {
        $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp })
            .done(function () {
                writeLine("started transport: " + connection.transport.name + " " + connection.id);

                stopStartBtn.prop("disabled", false)
                            .find("span")
                                .text("Stop Connection")
                                .end()
                            .find("i")
                                .removeClass("icon-play")
                                .addClass("icon-stop");

            });
    };
    start();


    $('#joinGroup').click(function () {
        // Set the connection Id to the specified value or the generated SignalR value
        var connectionIdToJoin = connectionTextInput.val() || $.connection.hub.id;

        hubConnectionAPI.server.joinGroup(connectionIdToJoin, groupNameTextInput.val()).done(function (value1) {
            writeLine("Succeeded at joinGroup: " + value1);
        }).fail(function (e) {
            writeError("Failed at joinGroup: " + e);
        });
    });

    $('#leaveGroup').click(function () {
        // Set the connection Id to the specified value or the generated SignalR value
        var connectionIdToLeave = connectionTextInput.val() || $.connection.hub.id;

        hubConnectionAPI.server.leaveGroup(connectionIdToLeave, groupNameTextInput.val()).done(function (value1) {
            writeLine("Succeeded at leaveGroup: " + value1);
        }).fail(function (e) {
            writeError("Failed at leaveGroup: " + e);
        });
    });

    $("#broadcast").click(function () {
        hubConnectionAPI.server.displayMessageAll(messageTextInput.val()).fail(function (e) {
            writeError("Failed at getMessage: " + e);
        });
    });

    $("#broadcastExceptSpecified").click(function () {
        hubConnectionAPI.server.displayMessageAllExcept(messageTextInput.val(), connectionTextInput.val().split(",")).fail(function (e) {
            writeError("Failed at getMessageAllExcept: " + e);
        });
    });


    $("#other").click(function () {
        hubConnectionAPI.server.displayMessageOther(messageTextInput.val()).fail(function (e) {
            writeError("Failed at getMessageOther: " + e);
        });
    });


    $("#sendToMe").click(function () {
        hubConnectionAPI.server.displayMessageCaller(meTextInput.val()).fail(function (e) {
            writeError("Failed at getMessageCaller: " + e);
        });
    });


    $("#specified").click(function () {
        hubConnectionAPI.server.displayMessageSpecified(connectionTextInput.val(), meTextInput.val()).fail(function (e) {
            writeError("Failed at getMessageSpecified: " + e);
        });
    });

    $("#groupmsg").click(function () {
        hubConnectionAPI.server.displayMessageGroup(groupNameTextInput.val(), groupMessageTextInput.val()).fail(function (e) {
            writeError("Failed at getMessageGroup: " + e);
        });
    });

    $("#groupmsgExceptSpecified").click(function () {
        hubConnectionAPI.server.displayMessageGroupExcept(groupNameTextInput.val(), groupMessageTextInput.val(), connectionTextInput.val().split(",")).fail(function (e) {
            writeError("Failed at displayMessageGroupExcept: " + e);
        });
    });

    $("#otherInGroupmsg").click(function () {
        hubConnectionAPI.server.displayMessageOthersInGroup(groupNameTextInput.val(), groupMessageTextInput.val()).fail(function (e) {
            writeError("Failed at displayMessageOthersInGroup: " + e);
        });
    });

    stopStartBtn.click(function () {
        var $el = $(this);

        $el.prop("disabled", true);

        if ($.trim($el.find("span").text()) === "Stop Connection") {
            $.connection.hub.stop();
        } else {
            start();
        }
    });
});
