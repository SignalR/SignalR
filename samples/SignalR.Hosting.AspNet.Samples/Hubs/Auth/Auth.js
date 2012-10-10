$(function () {
    var hubs = {
        'Messages': $.connection.noAuthHub,
        'Messages Invoked By Admin or Invoker': $.connection.invokeAuthHub,
        'Messages Requiring Authentication to Send or Receive': $.connection.authHub,
        'Messages Requiring Authentication to Send or Receive Because of Inheritance': $.connection.inheritAuthHub,
        'Messages Requiring Authentication to Send': $.connection.incomingAuthHub,
        'Messages Requiring Authentication to Receive': $.connection.outgoingAuthHub,
        'Messages Requiring Admin Membership to Send or Receive': $.connection.adminAuthHub,
        'Messages Requiring Name to be "User" and Role to be "Admin" to Send or Receive': $.connection.userAndRoleAuthHub
    };

    $.each(hubs, function (title, hub) {
        var list = $('<ul>').appendTo($('body').append('<h2>' + title + '</h2>'));
        var addMessage = function (value, color) {
            list.append('<li style="background-color:' + color + ';color:white">' + value + '</li>');
        };

        $.extend(hub.client, {
            joined: function (id, when, authInfo) {
                if ($.connection.hub.id === id) {
                    addMessage(id, 'blue');
                }
                var info = [];
                $.each(authInfo, function (k, v) {
                    info.push("<b>" + k + ":</b> " + v);
                });
                addMessage(id + ' joined at ' + when + '<br />' + info.join('; '), 'green');
            },
            rejoined: function (id, when) {
                addMessage(id + ' reconnected at ' + when, 'purple');
            },
            left: function (id, when) {
                addMessage(id + ' left at ' + when, 'red');
            },
            invoked: function (id, when) {
                addMessage(id + ' invoked hub method at' + when, 'orange');
            }
        });
    });

    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp }).done(function () {
        $.each(hubs, function () {
            this.server.invokedFromClient().fail($.connection.hub.log);
        });
    });
});