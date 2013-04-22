﻿/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";
    var myHub = $.connection.messagegsLoops;
    $.connection.hub.logging = true;

    var start = function () {          
            $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp })
                .done(function () {                         
                    $("<li/>").html("started transport: " + $.connection.hub.transport.name + " " + $.connection.hub.id)
                        .appendTo($("#messages"));  
                    
                    $("#stopStart")
                           .prop("disabled", false)
                           .find("span")
                               .text("Stop Connection")
                               .end()
                           .find("i")
                               .removeClass("icon-play")
                               .addClass("icon-stop");
                });                
        };
    start();

            
    var preValures = [];
    var missiedMessageCount = 0;
    var dupMessageCount = 0;

    var sendMessageCountHandler = function (value, connectionId) {                
        myHub.server.sendMessageCount(value, connectionId).done(function (value) {
            sendMessageCountHandler(value, connectionId)
        }).fail(function (e) {
            $("<li/>").html("failed at sendMessageCount: " + e).appendTo($("#messages"));
        });
    };
                
    myHub.client.fooCount = function (value, connectionId) {
               
        var firstReceive = true;
        var preValureItem;

        for (var preValure in preValures ) {
                    
            if (preValures[preValure].connectionId == connectionId)
            {
                firstReceive = false;
                preValureItem = preValures[preValure];
            }
       
        }

        if (firstReceive == true)
        {
            preValures.push({"connectionId": connectionId, "preValure" : value}) ;
            $("#message").append("<label id=" + connectionId + ">" + " </label>");
        }

        if (firstReceive == false) {
            if (value != (preValureItem.preValure + 1)) {
                if (value == preValureItem.preValure) {
                    $("<li/>").css("background-color", "yellow")
                    .css("color", "white")
                    .html("dup message in send messages loop: pre value: " + preValureItem.preValure + " current value: " + value + " from connectionId: " + connectionId)
                    .appendTo($("#messages"));

                    dupMessageCount++;

                }
                else {
                    $("<li/>").css("background-color", "red")
                            .css("color", "white")
                            .html("miss message in send messages loop: pre value: " + preValureItem.preValure + " current value: " + value + " from connectionId: " + connectionId)
                            .appendTo($("#messages"));

                    missiedMessageCount += value - (preValureItem.preValure + 1);
                }
                $("#missedMessagesCount").text("dup messages count: " + dupMessageCount + ", missed messages count: " + missiedMessageCount);
                                                
            }

            preValureItem.preValure = value;
        }

        $("#" + connectionId).text("messages loops "  + value + " from connectionId: " + connectionId );

                
    }

    $("#sendMessageCount").click(function () {
        $("#sendMessageCount").prop("disabled", true);
        myHub.server.sendMessageCount(0, $.connection.hub.id).done(function (value) {
            sendMessageCountHandler(value, $.connection.hub.id);
        }).fail(function (e) {
            $("<li/>").html("failed at sendMessageCount: " + e).appendTo($("#messages"));
            $("#sendMessageCount").prop("disabled", false);
        });
    });

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
                    .appendTo($("#messages"));                
    });

    $.connection.hub.disconnected(function () {
        $("#stopStart")
                    .prop("disabled", false)
                    .find("span")
                        .text("Start Connection")
                        .end()
                    .find("i")
                        .removeClass("icon-stop")
                        .addClass("icon-play");
    });

    $("#stopStart").click(function () {
        var $el = $(this);

        $el.prop("disabled", true);

        if ($.trim($el.find("span").text()) === "Stop Connection") {
            $.connection.hub.stop();
        } else {
            start();
        }
    });

});
            
 
