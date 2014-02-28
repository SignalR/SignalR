/// <reference path="../../Scripts/jquery-1.8.2.js" />
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

    $.connection.hub.logging = true;

    hubConnectionAPI.client.displayMessage = function (value) {
        $("<li/>").html("[" + new Date().toTimeString() + "]: " + value).appendTo(messages);
    }

    $.connection.hub.stateChanged(function (change) {
        var oldState = null,
            newState = null;

        for (var state in $.signalR.connectionState) {
            if ($.signalR.connectionState[state] === change.oldState) {
                oldState = state;
            } else if ($.signalR.connectionState[state] === change.newState) {
                newState = state;
            }
        }

        $("<li/>").html("[" + new Date().toTimeString() + "]: " + oldState + " => " + newState + " " + $.connection.hub.id)
                    .appendTo(messages);
    });

    $.connection.hub.reconnected(function () {
        $("<li/>").css("background-color", "green")
                    .css("color", "white")
                    .html("[" + new Date().toTimeString() + "]: Connection re-established")
                    .appendTo(messages);
        $("<li/>").html("reconnected transport: " + $.connection.hub.transport.name + " " + $.connection.hub.id)
                    .appendTo(messages);
    });

    $.connection.hub.error(function (err) {
        $("<li/>").html("Error occurred: " + err).appendTo(messages);
    });

    $.connection.hub.connectionSlow(function () {
        $("<li/>").html("[" + new Date().toTimeString() + "]: Connection Slow").appendTo(messages);
    });

    $.connection.hub.disconnected(function () {
        stopStartBtn.prop("disabled", false)
                    .find("span")
                        .text("Start Connection")
                        .end()
                    .find("i")
                        .removeClass("icon-stop")
                        .addClass("icon-play");
    });

    start = function () {
        $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp })
            .done(function () {
                $("<li/>").html("started transport: " + $.connection.hub.transport.name + " " + $.connection.hub.id)
                            .appendTo(messages);

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
            $("<li/>").html("Succeeded at joinGroup: " + value1).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at joinGroup: " + e).appendTo(messages);
        });
    });

    $('#leaveGroup').click(function () {
        // Set the connection Id to the specified value or the generated SignalR value
        var connectionIdToLeave = connectionTextInput.val() || $.connection.hub.id;

        hubConnectionAPI.server.leaveGroup(connectionIdToLeave, groupNameTextInput.val()).done(function (value1) {
            $("<li/>").html("Succeeded at leaveGroup: " + value1).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at leaveGroup: " + e).appendTo(messages);
        });
    });

    $("#broadcast").click(function () {
        hubConnectionAPI.server.displayMessageAll(messageTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at getMessage: " + e).appendTo(messages);
        });
    });

    $("#broadcastExceptSpecified").click(function () {
        hubConnectionAPI.server.displayMessageAllExcept(messageTextInput.val(), connectionTextInput.val().split(",")).fail(function (e) {
            $("<li/>").html("Failed at getMessageAllExcept: " + e).appendTo(messages);
        });
    });


    $("#other").click(function () {
        hubConnectionAPI.server.displayMessageOther(messageTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageOther: " + e).appendTo(messages);
        });
    });


    $("#sendToMe").click(function () {
        hubConnectionAPI.server.displayMessageCaller(meTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageCaller: " + e).appendTo(messages);
        });
    });


    $("#specified").click(function () {
        hubConnectionAPI.server.displayMessageSpecified(connectionTextInput.val(), meTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageSpecified: " + e).appendTo(messages);
        });
    });

    $("#groupmsg").click(function () {
        hubConnectionAPI.server.displayMessageGroup(groupNameTextInput.val(), groupMessageTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageGroup: " + e).appendTo(messages);
        });
    });

    $("#groupmsgExceptSpecified").click(function () {
        hubConnectionAPI.server.displayMessageGroupExcept(groupNameTextInput.val(), groupMessageTextInput.val(), connectionTextInput.val().split(",")).fail(function (e) {
            $("<li/>").html("Failed at displayMessageGroupExcept: " + e).appendTo(messages);
        });
    });

    $("#otherInGroupmsg").click(function () {
        hubConnectionAPI.server.displayMessageOthersInGroup(groupNameTextInput.val(), groupMessageTextInput.val()).fail(function (e) {
            $("<li/>").html("Failed at displayMessageOthersInGroup: " + e).appendTo(messages);
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
