/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    'use strict';

    var status = $.connection.StatusHub,
        startButton = $("#connectionStart"),
        stopButton = $("#connectionStop"),
        messagesHolder = $("#messages"),
        pingButton = $("#ping");

    function addMessage(value, className) {
        var html = $('<li>' + value + '</li>').addClass(className);
        messagesHolder.append(html);
    }

    function clearMessages() {
        messagesHolder.html("");
    }

    status.client.joined = function (id, when) {
        if ($.connection.hub.id === id) {
            addMessage('This connection: ' + id, 'text-info');
        }

        addMessage(id + ' joined at ' + when, 'text-success');
    };

    status.client.rejoined = function (id, when) {
        addMessage(id + ' reconnected at ' + when, 'text-warning');
    };

    status.client.leave = function (id, when) {
        addMessage(id + ' left at ' + when, 'text-error');
    };

    status.client.pong = function () {
        addMessage("Pong");
    }

    $.connection.hub.logging = true;

    stopButton.click(function () {
        $.connection.hub.stop();

        clearMessages();

        $(stopButton).attr('disabled', 'disabled');
        $(startButton).removeAttr('disabled');
        $(pingButton).attr('disabled', 'disabled');
        $(pingButton).unbind('click');
    });

    startButton.click(function () {
        $.connection.hub.start({ transport: activeTransport }).done(function () {
            $(pingButton).click(function () {
                status.server.ping();
            });
        });

        $(startButton).attr('disabled', 'disabled');
        $(stopButton).removeAttr('disabled');
        $(pingButton).removeAttr('disabled');
    });

    // Start Connection
    startButton.click();
});