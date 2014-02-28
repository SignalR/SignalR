/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    "use strict";

    var hubConnectionsCount = getQueryVariable('hubConnections'),
        persistentConnectionsCount = getQueryVariable('persistentConnections'),
        connections = [],
        messages = $("#messages"),
        connectionsCtrl = $("#connections"),
        start,
        addHubConnections,
        addPersistentConnections,
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
                $("<li/>").html("[" + new Date().toTimeString() + "]: connection" + connectionNumber + " reconnecting").prependTo(messages);
            });

            connection.reconnected(function () {
                $("<li/>").css("background-color", "green")
                          .css("color", "white")
                          .html("[" + new Date().toTimeString() + "]: connection" + connectionNumber + " Connection re-established")
                          .prependTo(messages);
            });

            connection.error(function (err) {
                $("<li/>").html("[" + new Date().toTimeString() + "]: connection" + connectionNumber + " " + (err.responseText || err))
                          .prependTo(messages);
            });

            connection.disconnected(function () {
                if (connections[connectionNumber].myHub) {
                    $("#stopStart" + connectionNumber)
                        .prop("disabled", false)
                        .val("Start Hub Connection" + connectionNumber)
                            .end();
                }
                else {
                    $("#stopStart" + connectionNumber)
                        .prop("disabled", false)
                        .val("Start Persistent Connection" + connectionNumber)
                            .end();
                }
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

                $("#connectionState" + connectionNumber).text(" (" + newState + ",");

                $("<li/>").html("[" + new Date().toTimeString() + "]: connection" + connectionNumber + " " + oldState + " => " + newState + " " + connection.id)
                          .prependTo(messages);
            });

            return connection;
        };


    start = function (connectionNumber) {
        var startConnection;

        if (connections[connectionNumber].myHub) {
            connections[connectionNumber].myHub.on('displayMessage', function (value) {
                $("<li/>").html("[" + new Date().toTimeString() + "]: connection" + connectionNumber + " " + value).prependTo(messages);

                $("#connectionMsg" + connectionNumber).text(value);
            });

            $("#broadcast" + connectionNumber).click(function () {
                connections[connectionNumber].myHub.invoke("displayMessageAll", $("#msg" + connectionNumber).val());
            });

        }
        else {
            connections[connectionNumber].connection.received(function (data) {
                $("<li/>").html("[" + new Date().toTimeString() + "]: " + window.JSON.stringify(data)).prependTo(messages);

                $("#connectionMsg" + connectionNumber).text(window.JSON.stringify(data));
            });

            $("#broadcast" + connectionNumber).click(function () {
                connections[connectionNumber].connection.send(window.JSON.stringify({ type: 1, value: $("#msg" + connectionNumber).val() }));
            });
        }

        $("#stopStart" + connectionNumber).click(function () {
            var $el = $("#stopStart" + connectionNumber);

            $el.prop("disabled", true);

            if (connections[connectionNumber].myHub) {
                if ($.trim($el.val()) === "Stop Hub Connection" + connectionNumber) {
                    connections[connectionNumber].connection.stop();
                } else {
                    startConnection();
                }
            }
            else {
                if ($.trim($el.val()) === "Stop Persistent Connection" + connectionNumber) {
                    connections[connectionNumber].connection.stop();
                } else {
                    startConnection();
                }
            }
        });

        startConnection = function () {
            connections[connectionNumber].connection.start({ transport: activeTransport, jsonp: isJsonp })
                .done(function () {
                    $("#connection" + connectionNumber).text(" " + connections[connectionNumber].connection.id + ", " + connections[connectionNumber].connection.transport.name + ")");

                    if (connections[connectionNumber].myHub) {
                        $("#stopStart" + connectionNumber).prop("disabled", false).val("Stop Hub Connection" + connectionNumber);
                    }
                    else {
                        $("#stopStart" + connectionNumber).prop("disabled", false).val("Stop Persistent Connection" + connectionNumber);
                    }
                });
        };

        startConnection();
    };

    addHubConnections = function (first, connectionsCount) {
        var connection, myHub;

        for (var i = first; i < first + connectionsCount; i++) {
            connectionsCtrl.append("<p>");
            connectionsCtrl.append("<input type='text' id='msg" + i + "' value=" + i + i + i + " style='margin-bottom: 0'></input>");
            connectionsCtrl.append("<input type='button' id='broadcast" + i + "' class='btn' value='Broadcast' ></input>");
            connectionsCtrl.append("<input id='stopStart" + i + "' type='button' class='btn' disabled='disabled' value='Stop Hub Connection'" + i + "'></input>");
            connectionsCtrl.append("<span id='connectionState" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<span id='connection" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<br/><b>The latest message received: </b><span id='connectionMsg" + i + "' style='display: inline'></span>");

            connection = createConnection(i);

            myHub = connection.createHubProxy('hubConnectionAPI');

            connections.push({ "connection": connection, "myHub": myHub });

            start(i);
        }
    }

    addPersistentConnections = function (first, connectionsCount) {
        var connection;

        for (var i = first; i < first + connectionsCount; i++) {
            connectionsCtrl.append("<p>");
            connectionsCtrl.append("<input type='text' id='msg" + i + "' value=" + i + i + i + " style='margin-bottom: 0'></input>");
            connectionsCtrl.append("<input type='button' id='broadcast" + i + "' class='btn' value='Broadcast' ></input>");
            connectionsCtrl.append("<input id='stopStart" + i + "' type='button' class='btn' disabled='disabled' value='Stop Persistent Connection'" + i + "'></input>");
            connectionsCtrl.append("<span id='connectionState" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<span id='connection" + i + "' style='display: inline'></span>");
            connectionsCtrl.append("<br/><b>The latest message received: </b><span id='connectionMsg" + i + "' style='display: inline'></span>");

            connection = createConnection(i, "../../raw-connection");

            connections.push({ "connection": connection });

            start(i);
        }
    }

    addHubConnections(0, parseInt(hubConnectionsCount));

    addPersistentConnections(connections.length, parseInt(persistentConnectionsCount));

    $("#addHubCon").click(function () {
        addHubConnections(connections.length, 1);
    });

    $("#addPCon").click(function () {
        addPersistentConnections(connections.length, 1);
    });
});


