

$(function () {
    "use strict";
                
    var myHub = $.connection.hubClientsAPIs;
    $.connection.hub.logging = true;

    myHub.client.foo = function (value) {                    
        $("<li/>").html("[" + new Date().toTimeString() + "]: " + value).appendTo($("#messages"));
    }
                
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

        $("<li/>").html("[" + new Date().toTimeString() + "]: " + oldState + " => " + newState + " "  + $.connection.hub.id )
                    .appendTo($("#messages"));                    
    });

    $.connection.hub.reconnected(function () {
        $("<li/>").css("background-color", "green")
                    .css("color", "white")
                    .html("[" + new Date().toTimeString() + "]: Connection re-established")
                    .appendTo($("#messages"));
        $("<li/>").html("reconnected transport: " + $.connection.hub.transport.name + " " + $.connection.hub.id)
                    .appendTo($("#messages"));
    });

    $.connection.hub.error(function (err) {
        $("<li/>").html("Error occurred: " + err )
                    .appendTo($("#messages"));
    });
                                
    $.connection.hub.connectionSlow(function () {
        $("<li/>").html("[" + new Date().toTimeString() + "]: Connection Slow")
                    .appendTo($("#messages"));
    });

    $('#join-group').click(function () {
        var connectionIdToJoin = $.connection.hub.id;

        if ($("#connection").val())
            var connectionIdToJoin = $("#connection").val();

        myHub.server.joinGroup(connectionIdToJoin, $("#group").val()).done(function (value1) {
            $("<li/>").html("success at join group: " + value1)
            .appendTo($("#messages"));
        }).fail(function (e) {
            $("<li/>").html("failed at join group: " + e)
            .appendTo($("#messages"));
        });
    });


    $('#leave-group').click(function () {
        var connectionIdToLeave = $.connection.hub.id;

        if ($("#connection").val())
            var connectionIdToLeave = $("#connection").val();

        myHub.server.leaveGroup(connectionIdToLeave, $("#group").val()).done(function (value1) {
            $("<li/>").html("success at leave-group: " + value1)
                .appendTo($("#messages"));
        }).fail(function (e) {
            $("<li/>").html("failed at leave-group: " + e)
            .appendTo($("#messages"));
        });
    });

    $("#broadcast").click(function () {
        myHub.server.getMessageAll($.connection.hub.id, window.JSON.stringify($("#msg").val())).fail(function (e) {
            $("<li/>").html("failed at getMessage: " + e)
            .appendTo($("#messages"));
        });
    });

    $("#broadcast-except-specified").click(function () {
        myHub.server.getMessageAllExcept($.connection.hub.id, window.JSON.stringify($("#msg").val()), $("#connection").val().split(",")).fail(function (e) {
            $("<li/>").html("failed at getMessageAllExcept: " + e)
            .appendTo($("#messages"));
        });                  
    });

                
    $("#other").click(function () {
        myHub.server.getMessageOther($.connection.hub.id, window.JSON.stringify($("#msg").val())).fail(function (e) {
            $("<li/>").html("failed at getMessageOther: " + e)
            .appendTo($("#messages"));
        });
    });


    $("#sendToMe").click(function () {                  
        myHub.server.getMessageCaller($.connection.hub.id, window.JSON.stringify($("#me").val())).fail(function (e) {
            $("<li/>").html("failed at getMessageCaller: " + e)
            .appendTo($("#messages"));
        });
    });


    $("#specified").click(function () {
        myHub.server.getMessageSpecified($.connection.hub.id, $("#connection").val(), window.JSON.stringify($("#me").val())).fail(function (e) {
            $("<li/>").html("failed at getMessageSpecified: " + e).appendTo($("#messages"));
        });
    });

    $("#groupmsg").click(function () {
        myHub.server.getMessageGroup($.connection.hub.id, $("#group").val(), window.JSON.stringify($("#groupMessage").val())).fail(function (e) {
            $("<li/>").html("failed at getMessageGroup: " + e).appendTo($("#messages"));
        });
    });

    $("#otherInGroupmsg").click(function () {
        myHub.server.getMessageOthersInGroup($.connection.hub.id, $("#group").val(), window.JSON.stringify($("#groupMessage").val())).fail(function (e) {
            $("<li/>").html("failed at getMessageOthersInGroup: " + e).appendTo($("#messages"));
        });
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
