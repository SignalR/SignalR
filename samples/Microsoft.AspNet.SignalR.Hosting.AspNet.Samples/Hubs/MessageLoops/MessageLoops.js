/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";
    var messageLoopsHub = $.connection.messageLoops,
        messages = $("#messages"),
        startMessageLoopsBtn = $("#startMessageLoops"),
        stopStartBtn = $("#stopStart"),
        start,
        sendMessageCountHandler,
        previousValues = [],
        missingMessageCount = 0,
        dupMessageCount = 0;

    $.connection.hub.logging = true;

    messageLoopsHub.client.displayMessagesCount = function (value, connectionId) {
        var firstReceive = true,
            previousValueItem;

        for (var i = 0; i < previousValues.length; i++) {
            if (previousValues[i].connectionId === connectionId) {
                firstReceive = false;
                previousValueItem = previousValues[i];
            }
        }
        
        if (firstReceive === true) {
            // if client receives message first time from the connectionId, then don't check missing /dup message
            previousValues.push({ "connectionId": connectionId, "previousValue": value });
            $("#messageLoops").append("<label id=" + connectionId + ">" + " </label>");
        } else {
            // check missing /dup message, and display if happens
            if (value !== (previousValueItem.previousValue + 1)) {
                if (value === previousValueItem.previousValue) {
                    $("<li/>").css("background-color", "yellow")
                    .css("color", "black")
                    .html("Duplicated message in messages loops: pre value: " + previousValueItem.previousValue + " current value: " + value + " from connectionId: " + connectionId)
                    .appendTo(messages);

                    dupMessageCount++;
                }
                else {
                    $("<li/>").css("background-color", "red")
                            .css("color", "white")
                            .html("Missing message in messages loops: pre value: " + previousValueItem.previousValue + " current value: " + value + " from connectionId: " + connectionId)
                            .appendTo(messages);

                    missingMessageCount += value - (previousValueItem.previousValue + 1);
                }
                $("#missingMessagesCount").text("Duplicated messages count: " + dupMessageCount + ", missing messages count: " + missingMessageCount);
            }

            previousValueItem.previousValue = value;
        }

        $("#" + connectionId).text("Messages loops " + value + " from connectionId: " + connectionId);
    }

    sendMessageCountHandler = function (value, connectionId) {
        messageLoopsHub.server.sendMessageCount(value, connectionId).done(function (value) {
            sendMessageCountHandler(value, connectionId)
        }).fail(function (e) {
            $("<li/>").html("Failed at sendMessageCount: " + e).appendTo(messages);
        });
    };

    $.connection.hub.stateChanged(function (change) {
        var oldState = null,
            newState = null;

        for (var p in $.signalR.connectionState) {
            if ($.signalR.connectionState[p] === change.oldState) {
                oldState = p;
            }
            if ($.signalR.connectionState[p] === change.newState) {
                newState = p;
            }
        }

        $("<li/>").html("[" + new Date().toTimeString() + "]: " + oldState + " => " + newState + " " + $.connection.hub.id)
                    .appendTo(messages);
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
                $("<li/>").html("Started transport: " + $.connection.hub.transport.name + " " + $.connection.hub.id)
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

    startMessageLoopsBtn.click(function () {
        startMessageLoopsBtn.prop("disabled", true);
        messageLoopsHub.server.sendMessageCount(0, $.connection.hub.id).done(function (value) {
            sendMessageCountHandler(value, $.connection.hub.id);
        }).fail(function (e) {
            $("<li/>").html("Failed at sendMessageCount: " + e).appendTo(messages);
            startMessageLoopsBtn.prop("disabled", false);
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


