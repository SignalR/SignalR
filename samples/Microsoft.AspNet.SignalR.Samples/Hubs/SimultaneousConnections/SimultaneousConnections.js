/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";

    var connectionsCount = getQueryVariable('cons'),
        connections = [],
        messages = $("#messages"),
        connectionsCtrl = $("#connections"),
        addConnections,
        start,
        createConnection = function (connectionNumber, persistentConnection) {
            var connection;

            if (persistentConnection) {
                connection = $.connection(persistentConnection);
            }
            else {
                connection = $.hubConnection();
            }

            connection.logging = true;

            connection.reconnecting(function () {
                $("<li/>").html("connection" + connectionNumber + " " + "[" + new Date().toTimeString() + "]: reconnecting").appendTo(messages);
            });

            connection.reconnected(function () {
                $("<li/>").css("background-color", "green")
                          .css("color", "white")
                          .html("connection" + connectionNumber + " " + "[" + new Date().toTimeString() + "]: Connection re-established")
                          .appendTo(messages);
            });

            connection.error(function (err) {
                $("<li/>").html("connection" + connectionNumber + " " + (err.responseText || err))
                          .appendTo(messages);
            });

            connection.disconnected(function () {
                $("#stopStart" + connectionNumber)
                    .prop("disabled", false)
                    .val("Start Connection" + connectionNumber)
                        .end();
            });

            connection.stateChanged(function (change) {
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

                $("#connectionState" + connectionNumber).text(" " + newState);

                $("<li/>").html("connection" + connectionNumber + " " + oldState + " => " + newState + " " + connection.id)
                          .appendTo(messages);
            });

            return connection;
        };


    start = function (connectionNumber) {
        var startConnection;

        connections[connectionNumber].myHub.on('displayMessage', function (value) {
            $("<li/>").html("connection" + connectionNumber + " " + value).appendTo(messages);

            $("#connectionMsg" + connectionNumber).text(value);
        });

        $("#stopStart" + connectionNumber).click(function () {
            var $el = $("#stopStart" + connectionNumber);

            $el.prop("disabled", true);

            if ($.trim($el.val()) === "Stop Connection" + connectionNumber) {
                connections[connectionNumber].connection.stop();
            } else {
                startConnection();
            }
        });

        $("#broadcast" + connectionNumber).click(function () {
            connections[connectionNumber].myHub.invoke("displayMessageAll", $("#msg" + connectionNumber).val());
        });

        startConnection = function () {
            connections[connectionNumber].connection.start({ transport: activeTransport, jsonp: isJsonp })
                .done(function () {
                    $("#connection" + connectionNumber).text(" " + connections[connectionNumber].connection.id + " " + connections[connectionNumber].connection.transport.name);

                    $("#stopStart" + connectionNumber).prop("disabled", false).val("Stop Connection" + connectionNumber);
                });
        };

        startConnection();
    };

    addConnections = function (first, connectionsCount) {
        var connection, myHub;

        for (var i = first; i < first + connectionsCount; i++) {
            connectionsCtrl.append("<p>");
            connectionsCtrl.append(" <input type='text' id='msg" + i + "' value=" + i + i + i + " style='margin-bottom: 0'></input>");
            connectionsCtrl.append("<input type='button' id='broadcast" + i + "' class='btn' value='Broadcast' ></input>");
            connectionsCtrl.append("<input id='stopStart" + i + "' type='button' class='btn' disabled='disabled' value='Stop Connection'" + i + "'></input>");
            connectionsCtrl.append("<span id='connectionState" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<span id='connection" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<br/><b>The latest message received: </b><span id='connectionMsg" + i + "' style='display: inline'></span>");

            connection = createConnection(i);

            myHub = connection.createHubProxy('hubConnectionAPI');

            connections.push({ "number": i, "connection": connection, "myHub": myHub, "start": start });

            connections[i].start(i);
        }
    }

    addConnections(0, connectionsCount);

    $("#addCon").click(function () {
        addConnections(connections.length, 1);
    });

});


