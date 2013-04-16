/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";

    var clientsAPIsHub = $.connection.hubClientsAPIs,
        messages = $("#messages"),
        stopStartBtn = $("#stopStart"),
        start;
    $.connection.hub.logging = true;

    clientsAPIsHub.client.displayMessage = function (value) {
        $("<li/>").html("[" + new Date().toTimeString() + "]: " + value).appendTo(messages);
    }

    $.connection.hub.stateChanged(function (change) {
        var oldState = null,
            newState = null;
        for (var p in $.signalR.connectionState) {
            if ($.signalR.connectionState[p] === change.oldState) {
                oldState = p;
            } else if ($.signalR.connectionState[p] === change.newState) {
                newState = p;
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


    $('#join-group').click(function () {
        var connectionIdToJoin = $.connection.hub.id;

        if ($("#connection").val())
            connectionIdToJoin = $("#connection").val();

        clientsAPIsHub.server.joinGroup(connectionIdToJoin, $("#group").val()).done(function (value1) {
            $("<li/>").html("Succeeded at joinGroup: " + value1).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at joinGroup: " + e).appendTo(messages);
        });
    });

    $('#leave-group').click(function () {
        var connectionIdToLeave = $.connection.hub.id;

        if ($("#connection").val())
            connectionIdToLeave = $("#connection").val();

        clientsAPIsHub.server.leaveGroup(connectionIdToLeave, $("#group").val()).done(function (value1) {
            $("<li/>").html("Succeeded at leaveGroup: " + value1).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at leaveGroup: " + e).appendTo(messages);
        });
    });

    $("#broadcast").click(function () {
        clientsAPIsHub.server.displayMessageAll($.connection.hub.id, $("#msg").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessage: " + e).appendTo(messages);
        });
    });

    $("#broadcast-except-specified").click(function () {
        clientsAPIsHub.server.displayMessageAllExcept($.connection.hub.id, $("#msg").val(), $("#connection").val().split(",")).fail(function (e) {
            $("<li/>").html("Failed at getMessageAllExcept: " + e).appendTo(messages);
        });
    });


    $("#other").click(function () {
        clientsAPIsHub.server.displayMessageOther($.connection.hub.id, $("#msg").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageOther: " + e).appendTo(messages);
        });
    });


    $("#sendToMe").click(function () {
        clientsAPIsHub.server.displayMessageCaller($.connection.hub.id, $("#me").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageCaller: " + e).appendTo(messages);
        });
    });


    $("#specified").click(function () {
        clientsAPIsHub.server.displayMessageSpecified($.connection.hub.id, $("#connection").val(), $("#me").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageSpecified: " + e).appendTo(messages);
        });
    });

    $("#groupmsg").click(function () {
        clientsAPIsHub.server.displayMessageGroup($.connection.hub.id, $("#group").val(), $("#groupMessage").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageGroup: " + e).appendTo(messages);
        });
    });

    $("#otherInGroupmsg").click(function () {
        clientsAPIsHub.server.displayMessageOthersInGroup($.connection.hub.id, $("#group").val(), $("#groupMessage").val()).fail(function (e) {
            $("<li/>").html("Failed at getMessageOthersInGroup: " + e).appendTo(messages);
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
