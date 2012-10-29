/// <reference path="../../Scripts/jquery-1.8.2.js" />
$(function () {
    'use strict';

    function addMessage(value, className) {
        var html = $('<li>' + value + '</li>').addClass(className);
        $('#messages').append(html);
    }

    var status = $.connection.StatusHub;

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

    $.connection.hub.start({ transport: activeTransport });
});