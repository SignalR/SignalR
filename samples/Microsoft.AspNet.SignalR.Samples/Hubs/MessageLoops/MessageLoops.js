/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";
    var messageLoopsHub = $.connection.messageLoops,
        messages = $("#messages"),
        startMessageLoopsBtn = $("#startMessageLoops"),
        stopStartBtn = $("#stopStart"),
        //default groupName
        groupName = "group++1",
        radioAll = $("#radioAll"),
        radioGroup = $("#radioGroup"),
        radioCaller = $("#radioCaller"),
        sleepInput = $("#sleep"),
        sendMessgeTo,
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
            previousValues.push({ "connectionId": connectionId, "previousValue": value, "isDup": false });
            $("#messageLoops").append("<label id=" + connectionId + ">" + " </label>");
        } else {
            // check missing /dup message, and display if happens
            if (value !== (previousValueItem.previousValue + 1)) {
                if (value === previousValueItem.previousValue) {
                    $("<li/>").css("background-color", "yellow")
                    .css("color", "black")
                    .html("[" + new Date().toTimeString() + "]: Duplicated message in message loops: pre value: " + previousValueItem.previousValue + " current value: " + value + " from connectionId: " + connectionId)
                    .appendTo(messages);

                    dupMessageCount++;

                    previousValueItem.isDup = true;
                } else {
                    $("<li/>").css("background-color", "red")
                            .css("color", "white")
                            .html("Missing message in message loops: pre value: " + previousValueItem.previousValue + " current value: " + value + " from connectionId: " + connectionId)
                            .appendTo(messages);

                    missingMessageCount += value - (previousValueItem.previousValue + 1);
                }
                $("#missingMessagesCount").text("Duplicated messages count: " + dupMessageCount + ", missing messages count: " + missingMessageCount);
            } else if (previousValueItem.isDup === true) {
                // Detected all duplicate message(s), now stop connection when new message is not duplicate, so we can easily invetigate it 
                $.connection.hub.stop();
            }

            previousValueItem.previousValue = value;
        }

        $("#" + connectionId).text("Message loops " + value + " from connectionId: " + connectionId);
    }

    sendMessageCountHandler = function (value) {
        if (sendMessgeTo === "all") {
            messageLoopsHub.server.sendMessageCountToAll(value, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at sendMessageCountToAll: " + e).appendTo(messages);
                sendMessageCountHandler(value + 1);
            });
        } else if (sendMessgeTo === "group") {
            messageLoopsHub.server.sendMessageCountToGroup(value, groupName, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at sendMessageCountToGroup: " + e).appendTo(messages);
                sendMessageCountHandler(value + 1);
            });
        } else if (sendMessgeTo === "caller") {
            messageLoopsHub.server.sendMessageCountToCaller(value, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at sendMessageCountToCaller: " + e).appendTo(messages);
                sendMessageCountHandler(value + 1);
            });
        }
    };

    function disableButtonsForMessageLoops(disable) {
        if (disable === true) {
            startMessageLoopsBtn.prop("disabled", true);
            radioAll.prop("disabled", true);
            radioGroup.prop("disabled", true);
            radioCaller.prop("disabled", true);
            sleepInput.prop("disabled", true);
        } else {
            startMessageLoopsBtn.prop("disabled", false);
            radioAll.prop("disabled", false);
            radioGroup.prop("disabled", false);
            radioCaller.prop("disabled", false);
            sleepInput.prop("disabled", false);
        }
    }

    $.connection.hub.stateChanged(function (change) {
        var oldState = null,
            newState = null;

        for (var state in $.signalR.connectionState) {
            if ($.signalR.connectionState[state] === change.oldState) {
                oldState = state;
            }
            if ($.signalR.connectionState[state] === change.newState) {
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
    });

    $.connection.hub.error(function (err) {
        $("<li/>").html("Error occurred: " + (err.responseText || err))
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

        disableButtonsForMessageLoops(false);
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

                disableButtonsForMessageLoops(false);

                if (radioGroup.prop("checked") === true) {
                    messageLoopsHub.server.joinGroup($.connection.hub.id, groupName).done(function (value) {
                        $("<li/>").html("Succeeded at joinGroup: " + value).appendTo(messages);
                    }).fail(function (e) {
                        $("<li/>").html("Failed at joinGroup: " + e).appendTo(messages);
                    });
                };

            });
    };
    start();

    radioAll.click(function () {
        messageLoopsHub.server.leaveGroup($.connection.hub.id, groupName).done(function (value) {
            $("<li/>").html("Succeeded at leaveGroup: " + value).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at leaveGroup: " + e).appendTo(messages);
        });
    });

    radioGroup.click(function () {
        messageLoopsHub.server.joinGroup($.connection.hub.id, groupName).done(function (value) {
            $("<li/>").html("Succeeded at joinGroup: " + value).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at joinGroup: " + e).appendTo(messages);
        });
    });

    radioCaller.click(function () {
        messageLoopsHub.server.leaveGroup($.connection.hub.id, groupName).done(function (value) {
            $("<li/>").html("Succeeded at leaveGroup: " + value).appendTo(messages);
        }).fail(function (e) {
            $("<li/>").html("Failed at leaveGroup: " + e).appendTo(messages);
        });
    });

    startMessageLoopsBtn.click(function () {
        disableButtonsForMessageLoops(true);

        if (radioAll.prop("checked") === true) {
            sendMessgeTo = "all";
            messageLoopsHub.server.sendMessageCountToAll(0, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at sendMessageCount: " + e).appendTo(messages);
                disableButtonsForMessageLoops(false);
            });
        } else if (radioGroup.prop("checked") === true) {
            sendMessgeTo = "group";
            messageLoopsHub.server.sendMessageCountToGroup(0, groupName, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at SendMessageCountToGroup: " + e).appendTo(messages);
                disableButtonsForMessageLoops(false);
            });
        } else if (radioCaller.prop("checked") === true) {
            sendMessgeTo = "caller";
            messageLoopsHub.server.sendMessageCountToCaller(0, parseInt(sleepInput.val())).done(function (value) {
                sendMessageCountHandler(value);
            }).fail(function (e) {
                $("<li/>").html("Failed at SendMessageCountToCaller: " + e).appendTo(messages);
                disableButtonsForMessageLoops(false);
            });
        }
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


